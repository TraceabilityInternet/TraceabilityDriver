using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    [TestFixture]
    public class SqlServerServiceTests
    {
        private IDatabaseService _dbService;
        private EPCISDocument _testEPCISDocument;
        private IDbContextFactory<ApplicationDbContext> _contextFactory;
        private bool _skipTests = false;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Check if tests should be skipped
            string skipSqlTests = Environment.GetEnvironmentVariable("NO_SQL_DB") ?? string.Empty;
            _skipTests = skipSqlTests.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

            if (_skipTests)
            {
                Assert.Ignore("SqlServer tests skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            OpenTraceability.GDST.Setup.Initialize();

            // Load configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            // Create a configuration with test collection names
            IConfiguration testConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            // setup context
            string connectionString = testConfig["SqlServer:ConnectionString"] ?? throw new Exception("SqlServer connection string not configured");
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString, x => x.EnableRetryOnFailure())
                .Options;

            _contextFactory = new PooledDbContextFactory<ApplicationDbContext>(options);
            ILogger<SqlServerService> logger = new LoggerFactory().CreateLogger<SqlServerService>();
            _dbService = new SqlServerService(logger, _contextFactory);

            // Clear out the data.
            await _dbService.ClearDatabaseAsync();

            // Initialize the database
            await _dbService.InitializeDatabase();

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

            // save all the events into the sql db service
            await _dbService.StoreEventsAsync(_testEPCISDocument.Events);

            // save all the master data into the mongo db service
            await _dbService.StoreMasterDataAsync(_testEPCISDocument.MasterData);
        }

        [Test]
        public void StoreEventsAsync_ShouldStoreEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
            }
            // This should pass via the setup method.
        }

        [Test]
        public void StoreMasterDataAsync_ShouldStoreMasterData()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
            }
            // This should pass via the setup method.
        }

        [Test]
        public async Task QueryEvents_WithTimeRange_ShouldReturnMatchingEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
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
            var result = await _dbService.QueryEvents(queryParams);

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
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
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
            var result = await _dbService.QueryEvents(queryParams);

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
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            await _dbService.StoreEventsAsync(_testEPCISDocument.Events);

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
            var result = await _dbService.QueryEvents(queryParams);

            // Assert
            Assert.That(result.Events, Is.Not.Null);
            Assert.That(result.Events, Is.Not.Empty);
            Assert.That(result.Events.All(e => e.BusinessStep.ToString() == testBizStep.ToString()), Is.True);
        }

        [Test]
        public async Task QueryMasterData_ShouldReturnMatchingElement()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            await _dbService.StoreMasterDataAsync(_testEPCISDocument.MasterData);

            // Get an ID from the test data
            var testElementId = _testEPCISDocument.MasterData.First().ID;

            // Act
            var result = await _dbService.QueryMasterData(testElementId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ID, Is.EqualTo(testElementId));
        }

        [Test]
        public async Task GetDatabaseReport_ShouldReturnAccurateReport()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            // Data should be already loaded via the LoadTestDataAsync method

            // Act
            var report = await _dbService.GetDatabaseReport();

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

        [Test]
        public async Task SaveSyncHistoryItem_ShouldStoreMemory()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            var syncHistoryItem = new SyncHistoryItem
            {
                Id = ObjectId.GenerateNewId().ToString(),
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
                Status = SyncStatus.Completed,
                Memory = new Dictionary<string, string>
                {
                    { "MaxID", "123" },
                },
            };

            // Act
            await _dbService.StoreSyncHistory(syncHistoryItem);

            // Assert
            using (var context = _contextFactory.CreateDbContext())
            {
                var dbItem = await context.SyncHistory
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == syncHistoryItem.Id);
                Assert.That(dbItem, Is.Not.Null);
                Assert.That(dbItem.Memory, Is.Not.Null);
                Assert.That(dbItem.Memory, Contains.Key("MaxID"));
                Assert.That(dbItem.Memory["MaxID"], Is.EqualTo("123"));
            }
        }
    }
}
