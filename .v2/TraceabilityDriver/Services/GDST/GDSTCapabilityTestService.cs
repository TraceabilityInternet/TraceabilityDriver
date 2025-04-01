using Microsoft.Extensions.Options;
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
        IMongoDBService _mongoDb;
        IHttpClientFactory _httpClientFactory;
        IOptions<GDSTCapabilityTestSettings> _settings;
        IConfiguration _config;

        public GDSTCapabilityTestService(ILogger<GDSTCapabilityTestService> logger, IMongoDBService mongoDb, IHttpClientFactory httpClientFactory, IOptions<GDSTCapabilityTestSettings> settings, IConfiguration config)
        {
            _logger = logger;
            _mongoDb = mongoDb;
            _httpClientFactory = httpClientFactory;
            _settings = settings;
            _config = config;
        }

        public async Task<bool> TestFirstMileWildAsync()
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

        public async Task<bool> ExecuteTestAsync()
        {
            JObject json = new JObject();
            json["SolutionName"] = _settings.Value.SolutionName;
            json["Version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            json["URL"] = _config["URL"] ?? throw new InvalidOperationException("URL is not set in the configuration.");
            json["PGLN"] = _settings.Value.PGLN;
            json["GDSTVersion"] = "12";
            json["EPCS"] = new JArray("urn:gdst:example.org:product:lot:class:processor.2u.v1-0122-2022");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", _settings.Value.ApiKey);
            var response = await client.PostAsync(_settings.Value.Url, new StringContent(json.ToString(), Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Test started successfully: {result}");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Test failed: {error}");
            }

            return false;
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

            // Store master data if needed
            // await _mongoDb.StoreMasterDataAsync(document.MasterData);
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
