using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Services;
using OpenTraceability.Mappers;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Tests.Models;

namespace TraceabilityDriver.Tests.Services
{
    [TestFixture]
    public class MongoDBServiceTests
    {
        private MongoDBService _mongoDBService = null!;
        private IConfiguration _configuration = null!;
        private EPCISDocument _testEPCISDocument = null!;
        private bool _skipTests = false;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Check if tests should be skipped
            string skipMongoDBTests = Environment.GetEnvironmentVariable("NO_MONGO_DB") ?? string.Empty;
            _skipTests = skipMongoDBTests.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            
            if (_skipTests)
            {
                Assert.Ignore("MongoDB tests skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            OpenTraceability.GDST.Setup.Initialize();

            // Load configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            // Create a configuration with test collection names
            var testConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            // Create service with test configuration
            _mongoDBService = new MongoDBService(testConfig, NullLogger<MongoDBService>.Instance);

            // Clear out the data.
            await _mongoDBService.ClearDatabaseAsync();

            // Load test data
            await LoadTestDataAsync();
        }

        private async Task LoadTestDataAsync()
        {
            if (_skipTests) return;

            // Load test data from JSON file
            string testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "testdata001.json");
            if (!File.Exists(testDataPath))
            {
                Assert.Fail($"Test data file not found at {testDataPath}");
            }

            string jsonData = File.ReadAllText(testDataPath);

            // deserialize the test data into an EPCISDocument
            _testEPCISDocument = OpenTraceabilityMappers.EPCISDocument.JSON.Map(jsonData);

            // save all the events into the mongo db service
            await _mongoDBService.StoreEventsAsync(_testEPCISDocument.Events);

            // save all the master data into the mongo db service
            await _mongoDBService.StoreMasterDataAsync(_testEPCISDocument.MasterData);
        }

        [Test]
        public void StoreEventsAsync_ShouldStoreEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
            }
            // This should pass via the setup method.
        }

        [Test]
        public void StoreMasterDataAsync_ShouldStoreMasterData()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
            }
            // This should pass via the setup method.
        }

        [Test]
        public async Task QueryEvents_WithTimeRange_ShouldReturnMatchingEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Determine start and end times via the _testEPCISDocument.
            var startTime = _testEPCISDocument.Events.Min(e => e.EventTime);
            var endTime = _testEPCISDocument.Events.Max(e => e.EventTime);
            
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery
                {
                    GE_eventTime = startTime,
                    LE_eventTime = endTime
                }
            };

            // Act
            var result = await _mongoDBService.QueryEvents(queryParams);

            // Assert
            Assert.That(result.Events, Is.Not.Null);
            Assert.That(result.Events.Count, Is.GreaterThan(0));
            Assert.That(result.Events.All(e => e.EventTime >= startTime && e.EventTime <= endTime), Is.True);
        }

        [Test]
        public async Task QueryEvents_WithEPC_ShouldReturnMatchingEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Arrange            
            var testEPC = _testEPCISDocument.Events.FirstOrDefault(e => e.Products.Count > 1)?.Products.First().EPC.ToString();
            Assert.That(testEPC, Is.Not.Null, "Test data should contain at least one event with an EPC");
            
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery
                {
                    MATCH_anyEPC = new List<string> { testEPC! }
                }
            };

            // Act
            var result = await _mongoDBService.QueryEvents(queryParams);

            // Assert
            Assert.That(result.Events, Is.Not.Null);
            Assert.That(result.Events, Is.Not.Empty);
            Assert.That(result.Events.Any(e => e.Products.Any(p => p.EPC.ToString() == testEPC)), Is.True);
        }

        [Test]
        public async Task QueryEvents_WithBizStep_ShouldReturnMatchingEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            await _mongoDBService.StoreEventsAsync(_testEPCISDocument.Events);
            
            // Get a business step from the test data
            var testBizStep = _testEPCISDocument.Events.First().BusinessStep;
            
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery
                {
                    EQ_bizStep = new List<string> { testBizStep.ToString() }
                }
            };

            // Act
            var result = await _mongoDBService.QueryEvents(queryParams);

            // Assert
            Assert.That(result.Events, Is.Not.Null);
            Assert.That(result.Events, Is.Not.Empty);
            Assert.That(result.Events.All(e => e.BusinessStep.ToString() == testBizStep.ToString()), Is.True);
        }

        /// <summary>
        /// EQ_bizLocation must match only the business location, never a source or destination location with the same GLN.
        /// </summary>
        [Test]
        public async Task QueryEvents_WithBizLocation_ReturnsOnlyEventsAtThatBizLocation()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            string targetGLN = BizLocationTestEventBuilder.CreateUniqueGLN();
            IEvent atBizLocation = BizLocationTestEventBuilder.CreateEvent(targetGLN);
            IEvent fromSourceLocation = BizLocationTestEventBuilder.CreateEvent(BizLocationTestEventBuilder.CreateUniqueGLN(), sourceLocationGLN: targetGLN);
            IEvent toDestinationLocation = BizLocationTestEventBuilder.CreateEvent(BizLocationTestEventBuilder.CreateUniqueGLN(), destinationLocationGLN: targetGLN);
            await _mongoDBService.StoreEventsAsync(new List<IEvent> { atBizLocation, fromSourceLocation, toDestinationLocation });

            EPCISQueryParameters queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery
                {
                    EQ_bizLocation = new List<Uri> { new Uri(targetGLN) }
                }
            };

            // Act
            EPCISQueryDocument result = await _mongoDBService.QueryEvents(queryParams);

            // Assert
            Assert.That(result.Events.Select(e => e.EventID?.ToString()), Is.EquivalentTo(new[] { atBizLocation.EventID!.ToString() }), "Only the event whose bizLocation is the GLN should match; source/destination matches are excluded.");
        }

        /// <summary>
        /// Documents stored before BizLocationGLN existed are backfilled at startup and then match EQ_bizLocation.
        /// </summary>
        [Test]
        public async Task InitializeDatabase_WithLegacyDocumentsMissingBizLocation_BackfillsAndQueryMatches()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            IMongoCollection<BsonDocument> rawEvents = GetRawEventsCollection();
            string targetGLN = BizLocationTestEventBuilder.CreateUniqueGLN();
            IEvent legacyEvent = BizLocationTestEventBuilder.CreateEvent(targetGLN, sourceLocationGLN: BizLocationTestEventBuilder.CreateUniqueGLN());
            BsonDocument legacyDocument = new EPCISEventDocument(legacyEvent).ToBsonDocument();
            legacyDocument.Remove(nameof(EPCISEventDocument.BizLocationGLN));
            await rawEvents.InsertOneAsync(legacyDocument);

            FilterDefinition<BsonDocument> byEventId = Builders<BsonDocument>.Filter.Eq(nameof(EPCISEventDocument.EventId), legacyEvent.EventID!.ToString());

            // Act
            await _mongoDBService.InitializeDatabase();

            // Assert
            BsonDocument stored = await rawEvents.Find(byEventId).FirstAsync();
            Assert.That(stored.Contains(nameof(EPCISEventDocument.BizLocationGLN)), Is.True, "The backfill should add the BizLocationGLN field.");
            Assert.That(stored[nameof(EPCISEventDocument.BizLocationGLN)].AsString, Is.EqualTo(targetGLN.ToLower()));

            EPCISQueryDocument result = await _mongoDBService.QueryEvents(new EPCISQueryParameters { query = new EPCISQuery { EQ_bizLocation = new List<Uri> { new Uri(targetGLN) } } });
            Assert.That(result.Events.Select(e => e.EventID?.ToString()), Is.EquivalentTo(new[] { legacyEvent.EventID.ToString() }));

            long remainingLegacy = await rawEvents.CountDocumentsAsync(Builders<BsonDocument>.Filter.Exists(nameof(EPCISEventDocument.BizLocationGLN), false));
            Assert.That(remainingLegacy, Is.EqualTo(0), "No documents should be left without the BizLocationGLN field.");
        }

        /// <summary>
        /// Opens the events collection as raw BSON so tests can write documents in the legacy shape.
        /// </summary>
        private static IMongoCollection<BsonDocument> GetRawEventsCollection()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.Tests.json").Build();
            MongoClient client = new MongoClient(config["MongoDB:ConnectionString"]);
            return client.GetDatabase(config["MongoDB:DatabaseName"]).GetCollection<BsonDocument>(config["MongoDB:EventsCollectionName"]);
        }

        [Test]
        public async Task QueryMasterData_ShouldReturnMatchingElement()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            await _mongoDBService.StoreMasterDataAsync(_testEPCISDocument.MasterData);
            
            // Get an ID from the test data
            var testElementId = _testEPCISDocument.MasterData.First().ID;

            // Act
            var result = await _mongoDBService.QueryMasterData(testElementId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ID, Is.EqualTo(testElementId));
        }

        [Test]
        public async Task GetDatabaseReport_ShouldReturnAccurateReport()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            // Data should be already loaded via the LoadTestDataAsync method

            // Act
            var report = await _mongoDBService.GetDatabaseReport();

            // Assert
            Assert.That(report, Is.Not.Null);

            // Event counts should not be empty if test data was loaded
            Assert.That(report.EventCounts, Is.Not.Empty, "Database report should contain event counts");

            // Master data counts should not be empty if test data was loaded
            Assert.That(report.MasterDataCounts, Is.Not.Empty, "Database report should contain master data counts");

            // Verify the actual counts match with our test data
            var expectedEventTypeCount = _testEPCISDocument.Events
                .GroupBy(e => e.BusinessStep.ToString().ToLower())
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var eventType in expectedEventTypeCount.Keys)
            {
                Assert.That(report.EventCounts, Contains.Key(eventType),
                    $"Database report should contain count for event type {eventType}");
                Assert.That(report.EventCounts[eventType], Is.EqualTo(expectedEventTypeCount[eventType]),
                    $"Event count for {eventType} should match expected value");
            }

            // Master data types would require similar verification
            var expectedMasterDataTypeCount = _testEPCISDocument.MasterData
                .GroupBy(m =>
                {
                    var typeName = m.GetType().ToString();
                    int lastDot = typeName.LastIndexOf('.');
                    return lastDot > 0 && lastDot < typeName.Length - 1
                        ? typeName.Substring(lastDot + 1)
                        : typeName;
                })
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var dataType in expectedMasterDataTypeCount.Keys)
            {
                Assert.That(report.MasterDataCounts, Contains.Key(dataType),
                    $"Database report should contain count for master data type {dataType}");
                Assert.That(report.MasterDataCounts[dataType], Is.EqualTo(expectedMasterDataTypeCount[dataType]),
                    $"Master data count for {dataType} should match expected value");
            }
        }
    }
} 