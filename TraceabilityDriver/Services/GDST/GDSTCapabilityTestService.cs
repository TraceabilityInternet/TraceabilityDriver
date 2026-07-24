using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using System.Reflection;
using System.Text;
using TraceabilityDriver.Models.GDST;
using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services.GDST
{
    /// <summary>
    /// A service that will facilitate taking the capability tests.
    /// </summary>
    /// <remarks>
    /// Runs the GDST capability tool's v2 flow: start the test (POST v2/process/start), trace back and
    /// ingest the tool-generated EPCs from the tool's own endpoints, advance the test
    /// (POST v2/process/next), and poll for the report (GET v2/process/report) until it leaves the
    /// Started status.
    /// </remarks>
    public class GDSTCapabilityTestService : IGDSTCapabilityTestService
    {
        ILogger<GDSTCapabilityTestService> _logger;
        IDatabaseService _mongoDb;
        IHttpClientFactory _httpClientFactory;
        ITracebackService _tracebackService;
        IOptions<GDSTCapabilityTestSettings> _settings;
        IConfiguration _config;

        public GDSTCapabilityTestService(ILogger<GDSTCapabilityTestService> logger, IDatabaseService mongoDb, IHttpClientFactory httpClientFactory, ITracebackService tracebackService, IOptions<GDSTCapabilityTestSettings> settings, IConfiguration config)
        {
            _logger = logger;
            _mongoDb = mongoDb;
            _httpClientFactory = httpClientFactory;
            _tracebackService = tracebackService;
            _settings = settings;
            _config = config;
        }

        public async Task<GDSTCapabilityTestResults> TestFirstMileWildAsync()
        {
            try
            {
                // Verify the capability test settings.
                if (_settings?.Value == null)
                {
                    throw new NullReferenceException("GDST capability test settings are not initialized.");
                }

                // Load the test data into the database
                await LoadTestDataIntoDatabaseAsync();

                // Perform the test
                return await ExecuteTestAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running the capability test.");
                return new GDSTCapabilityTestResults()
                {
                    Status = GDSTCapabilityTestStatus.Failed,
                    Errors = new List<GDSTCapabilityTestsError>()
                    {
                        new GDSTCapabilityTestsError() { Error = "An unknown error occurred while running the capability test." }
                    }
                };
            }
        }

        public async Task<GDSTCapabilityTestResults> ExecuteTestAsync()
        {
            string digitalLinkURL = _config["URL"]?.TrimEnd('/') + "/digitallink/";

            // if the app has an api key configured,
            // we need include it in the request to the capability tool
            string apiKey = "123";
            if (_config.GetSection("Authentication:APIKey").Exists())
            {
                List<string> validKeys = _config.GetSection("Authentication:APIKey:ValidKeys").Get<List<string>>() ?? new List<string>();
                if (validKeys.Any())
                {
                    apiKey = validKeys.First();
                }
            }

            GDSTCapabilityTestModel startModel = new GDSTCapabilityTestModel
            {
                SolutionName = _settings.Value.SolutionName,
                Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                ApiKey = apiKey,
                Url = digitalLinkURL,
                Pgln = _settings.Value.PGLN,
                GdstVersion = 20,
                SolutionProviderEPCs = new List<string>() { "urn:gdst:example.org:product:lot:class:processor.2u.v1-0122-2022" }
            };

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", _settings.Value.ApiKey);
            client.BaseAddress = new Uri(_settings.Value.Url);
            var response = await client.PostAsync("/v2/process/start", new StringContent(JsonConvert.SerializeObject(startModel), Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Test started successfully: {Result}", result);

                GDSTCapabilityTestModel model = JsonConvert.DeserializeObject<GDSTCapabilityTestModel>(result)
                    ?? throw new Exception("The response could not be deserialized.");

                if (string.IsNullOrWhiteSpace(model.ComplianceProcessUUID))
                {
                    throw new Exception("The start response did not include a compliance process UUID.");
                }

                if (!model.EpCs.Any())
                {
                    throw new Exception("The start response did not include any tool-generated EPCs to ingest.");
                }

                // The v2 test requires the solution to ingest the tool-generated data before advancing.
                await IngestGeneratedDataAsync(model);

                // Advance the test from WaitingForProvider to Processing so the tool runs the assessment.
                await AdvanceTestAsync(model);

                return await PollForResultsAsync(model);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"The request failed with status code {response.StatusCode}. Error: {error}");
            }
        }

        /// <summary>
        /// Traces back the tool-generated EPCs from the capability tool's own endpoints and stores the
        /// resulting events and master data in the database.
        /// </summary>
        /// <param name="model">The start response carrying the process UUID and the tool-generated EPCs.</param>
        /// <exception cref="Exception">Thrown when no events could be retrieved for the generated EPCs.</exception>
        private async Task IngestGeneratedDataAsync(GDSTCapabilityTestModel model)
        {
            // The process UUID header selects which capability test's generated data the tool serves.
            DigitalLinkQueryOptions options = new DigitalLinkQueryOptions
            {
                URL = new Uri(_settings.Value.Url.TrimEnd('/') + "/digitallink/"),
                APIKey = _settings.Value.ApiKey,
                Headers = { ["X-Capability-Process-UUID"] = model.ComplianceProcessUUID }
            };

            TracebackFetchResult result = await _tracebackService.TracebackAsync(model.EpCs, options, CancellationToken.None);

            foreach (string error in result.Errors)
            {
                _logger.LogWarning("An error occurred while tracing back the tool-generated data: {Error}", error);
            }

            if (!result.Document.Events.Any())
            {
                throw new Exception("No events could be traced back from the capability tool for the generated EPCs. The test cannot proceed without ingesting the generated data.");
            }

            await _mongoDb.StoreEventsAsync(result.Document.Events);
            await _mongoDb.StoreMasterDataAsync(result.Document.MasterData);

            _logger.LogInformation("Ingested {EventCount} event(s) and {MasterDataCount} master data element(s) generated by the capability tool.", result.Document.Events.Count, result.Document.MasterData.Count);
        }

        /// <summary>
        /// Advances the capability test from WaitingForProvider to Processing (POST v2/process/next).
        /// Must be called exactly once, after the generated data has been ingested.
        /// </summary>
        /// <param name="model">The start response carrying the process UUID.</param>
        /// <exception cref="Exception">Thrown when the tool rejects the request.</exception>
        private async Task AdvanceTestAsync(GDSTCapabilityTestModel model)
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", _settings.Value.ApiKey);
            client.DefaultRequestHeaders.Add("X-Capability-Process-UUID", model.ComplianceProcessUUID);
            client.BaseAddress = new Uri(_settings.Value.Url);

            var response = await client.PostAsync("/v2/process/next", new StringContent(string.Empty));
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"The request to advance the capability test failed with status code {response.StatusCode}. Error: {error}");
            }

            _logger.LogInformation("The capability test {ComplianceProcessUUID} was advanced to the processing stage.", model.ComplianceProcessUUID);
        }

        public async Task<GDSTCapabilityTestResults> PollForResultsAsync(GDSTCapabilityTestModel test)
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", _settings.Value.ApiKey);
            client.DefaultRequestHeaders.Add("X-Capability-Process-UUID", test.ComplianceProcessUUID);
            client.BaseAddress = new Uri(_settings.Value.Url);

            GDSTCapabilityTestResults results = new GDSTCapabilityTestResults()
            {
                Status = GDSTCapabilityTestStatus.Started
            };

            // Now we need to poll for up to 5 minutes to get the response.
            for (int i = 0; i < 300; i++)
            {
                await Task.Delay(1000);

                var response = await client.GetAsync($"/v2/process/report");
                if (response.IsSuccessStatusCode)
                {
                    var report = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation("Test results: {Report}", report);

                    results = JsonConvert.DeserializeObject<GDSTCapabilityTestResults>(report)
                        ?? throw new Exception("The response could not be deserialized.");

                    if (results.Status == GDSTCapabilityTestStatus.Started)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return results;
        }

        public async Task LoadTestDataIntoDatabaseAsync()
        {
            if (_mongoDb == null)
            {
                throw new InvalidOperationException("MongoDB service is not initialized.");
            }

            // Generate the traceability data
            var document = GenerateTraceabilityData();

            // Store the events
            await _mongoDb.StoreEventsAsync(document.Events);

            // Store master data
            await _mongoDb.StoreMasterDataAsync(document.MasterData);
        }

        public EPCISDocument GenerateTraceabilityData()
        {
            // Load the EPCIS document from the embedded resource.
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TraceabilityDriver.Services.GDST.FullData.json");
            if (stream == null)
            {
                throw new FileNotFoundException("The resource 'FullData.json' was not found.");
            }

            using var reader = new StreamReader(stream);

            // Read the JSON content.
            string json = reader.ReadToEnd();

            // Deserialize the JSON content into an EPCISDocument object.
            var document = OpenTraceability.Mappers.OpenTraceabilityMappers.EPCISDocument.JSON.Map(json);

            return document;
        }
    }
}
