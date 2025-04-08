using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTraceability.Models.Events;
using System.Reflection;
using System.Text;
using TraceabilityDriver.Models.GDST;

namespace TraceabilityDriver.Services.GDST
{
    /// <summary>
    /// A service that will facilitate taking the capability tests.
    /// </summary>
    public class GDSTCapabilityTestService : IGDSTCapabilityTestService
    {
        ILogger<GDSTCapabilityTestService> _logger;
        IDatabaseService _mongoDb;
        IHttpClientFactory _httpClientFactory;
        IOptions<GDSTCapabilityTestSettings> _settings;
        IConfiguration _config;

        public GDSTCapabilityTestService(ILogger<GDSTCapabilityTestService> logger, IDatabaseService mongoDb, IHttpClientFactory httpClientFactory, IOptions<GDSTCapabilityTestSettings> settings, IConfiguration config)
        {
            _logger = logger;
            _mongoDb = mongoDb;
            _httpClientFactory = httpClientFactory;
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

            JObject json = new JObject();
            json["SolutionName"] = _settings.Value.SolutionName;
            json["Version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            json["APIKey"] = apiKey;
            json["URL"] = digitalLinkURL;
            json["PGLN"] = _settings.Value.PGLN;
            json["GDSTVersion"] = "12";
            json["EPCS"] = new JArray("urn:gdst:example.org:product:lot:class:processor.2u.v1-0122-2022");

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", _settings.Value.ApiKey);
            client.BaseAddress = new Uri(_settings.Value.Url);
            var response = await client.PostAsync("/process/start", new StringContent(json.ToString(), Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Test started successfully: {Result}", result);

                GDSTCapabilityTestModel model = JsonConvert.DeserializeObject<GDSTCapabilityTestModel>(result)
                    ?? throw new Exception("The response could not be deserialized.");

                return await PollForResultsAsync(model);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"The request failed with status code {response.StatusCode}. Error: {error}");
            }
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

                var response = await client.GetAsync($"/process/report");
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
