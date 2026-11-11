using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Queries;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Models.Sql;
using TraceabilityDriver.Services;
using TraceabilityDriver.Tests.Models;

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

        /// <summary>
        /// EQ_bizLocation must match only the business location, never a source or destination location with the same GLN.
        /// </summary>
        [Test]
        public async Task QueryEvents_WithBizLocation_ReturnsOnlyEventsAtThatBizLocation()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            string targetGLN = BizLocationTestEventBuilder.CreateUniqueGLN();
            IEvent atBizLocation = BizLocationTestEventBuilder.CreateEvent(targetGLN);
            IEvent fromSourceLocation = BizLocationTestEventBuilder.CreateEvent(BizLocationTestEventBuilder.CreateUniqueGLN(), sourceLocationGLN: targetGLN);
            IEvent toDestinationLocation = BizLocationTestEventBuilder.CreateEvent(BizLocationTestEventBuilder.CreateUniqueGLN(), destinationLocationGLN: targetGLN);
            await _dbService.StoreEventsAsync(new List<IEvent> { atBizLocation, fromSourceLocation, toDestinationLocation });

            EPCISQueryParameters queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery
                {
                    EQ_bizLocation = new List<Uri> { new Uri(targetGLN) }
                }
            };

            // Act
            EPCISQueryDocument result = await _dbService.QueryEvents(queryParams);

            // Assert
            Assert.That(result.Events.Select(e => e.EventID?.ToString()), Is.EquivalentTo(new[] { atBizLocation.EventID!.ToString() }), "Only the event whose bizLocation is the GLN should match; source/destination matches are excluded.");
        }

        /// <summary>
        /// Search rows written before BizLocationGLN existed (NULL) are backfilled at startup and then match EQ_bizLocation.
        /// </summary>
        [Test]
        public async Task InitializeDatabase_WithLegacyRowsMissingBizLocation_BackfillsAndQueryMatches()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            string targetGLN = BizLocationTestEventBuilder.CreateUniqueGLN();
            IEvent legacyEvent = BizLocationTestEventBuilder.CreateEvent(targetGLN, sourceLocationGLN: BizLocationTestEventBuilder.CreateUniqueGLN());
            await _dbService.StoreEventsAsync(new List<IEvent> { legacyEvent });
            string legacyEventId = legacyEvent.EventID!.ToString();
            await SetBizLocationToNullAsync(legacyEventId);

            // Act
            await _dbService.InitializeDatabase();

            // Assert
            await AssertBackfilledAsync(legacyEventId, targetGLN);
        }

        /// <summary>
        /// A database created before the BizLocationGLN column existed is upgraded in place and its rows backfilled.
        /// </summary>
        [Test]
        public async Task InitializeDatabase_WithSchemaMissingBizLocationColumn_AddsColumnAndBackfills()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            string targetGLN = BizLocationTestEventBuilder.CreateUniqueGLN();
            IEvent legacyEvent = BizLocationTestEventBuilder.CreateEvent(targetGLN, destinationLocationGLN: BizLocationTestEventBuilder.CreateUniqueGLN());
            await _dbService.StoreEventsAsync(new List<IEvent> { legacyEvent });
            using (ApplicationDbContext context = await _contextFactory.CreateDbContextAsync())
            {
                await context.Database.ExecuteSqlRawAsync($"DROP INDEX {ApplicationDbContext.BizLocationGLNIndexName} ON EventSearchDocuments;");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE EventSearchDocuments DROP COLUMN BizLocationGLN;");
            }

            // Act
            await _dbService.InitializeDatabase();

            // Assert
            await AssertBackfilledAsync(legacyEvent.EventID!.ToString(), targetGLN);
        }

        /// <summary>
        /// Legacy rows whose bizLocation cannot be extracted, spread over several pages, must not stall the backfill: it
        /// finishes, marks each of them processed with an empty value, and still backfills the valid event on the last page.
        /// </summary>
        [Test]
        public async Task InitializeDatabase_WithUnextractableBizLocationsAcrossPages_CompletesAndBackfillsTheRest()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Arrange: two full pages plus one more, so the backfill has to page past failures more than once.
            int unextractableCount = (SqlServerService.BackfillBatchSize * 2) + 1;
            List<IEvent> unextractableEvents = Enumerable.Range(0, unextractableCount).Select(_ => BizLocationTestEventBuilder.CreateEvent(BizLocationTestEventBuilder.CreateUniqueGLN())).ToList();
            List<string> unextractableEventIds = unextractableEvents.Select(e => e.EventID!.ToString()).ToList();
            await _dbService.StoreEventsAsync(unextractableEvents);

            // The valid event is stored last so its search rows have the highest keys and land on the final page.
            string targetGLN = BizLocationTestEventBuilder.CreateUniqueGLN();
            IEvent validEvent = BizLocationTestEventBuilder.CreateEvent(targetGLN);
            string validEventId = validEvent.EventID!.ToString();
            await _dbService.StoreEventsAsync(new List<IEvent> { validEvent });

            List<string> insertedEventIds = unextractableEventIds.Append(validEventId).ToList();
            using (ApplicationDbContext context = await _contextFactory.CreateDbContextAsync())
            {
                await context.EventSearchDocuments.Where(x => insertedEventIds.Contains(x.EventId)).ExecuteUpdateAsync(s => s.SetProperty(x => x.BizLocationGLN, (string?)null));
                await context.EPCISEvents.Where(x => unextractableEventIds.Contains(x.EventId)).ExecuteUpdateAsync(s => s.SetProperty(x => x.EventJson, "this is not event JSON"));
            }

            try
            {
                // Act
                Task backfill = _dbService.InitializeDatabase();
                Task finished = await Task.WhenAny(backfill, Task.Delay(TimeSpan.FromSeconds(60)));
                Assert.That(finished, Is.SameAs(backfill), "The backfill did not finish within 60 seconds; it is likely stuck in a loop.");
                await backfill;

                // Assert
                using ApplicationDbContext context = await _contextFactory.CreateDbContextAsync();
                List<string?> unextractableValues = await context.EventSearchDocuments.Where(x => unextractableEventIds.Contains(x.EventId)).Select(x => x.BizLocationGLN).ToListAsync();
                Assert.That(unextractableValues, Is.Not.Empty);
                Assert.That(unextractableValues, Is.All.EqualTo(string.Empty), "Every row whose bizLocation could not be extracted should be marked processed with an empty value.");

                List<string?> validValues = await context.EventSearchDocuments.Where(x => x.EventId == validEventId).Select(x => x.BizLocationGLN).ToListAsync();
                Assert.That(validValues, Is.Not.Empty);
                Assert.That(validValues, Is.All.EqualTo(targetGLN.ToLower()), "The valid event after the failures should still be backfilled.");

                int remainingLegacy = await context.EventSearchDocuments.CountAsync(x => x.BizLocationGLN == null);
                Assert.That(remainingLegacy, Is.EqualTo(0), "No search rows should be left with a NULL BizLocationGLN.");
            }
            finally
            {
                // The unparseable events would break other tests that read every stored event.
                using ApplicationDbContext context = await _contextFactory.CreateDbContextAsync();
                await context.EventSearchDocuments.Where(x => insertedEventIds.Contains(x.EventId)).ExecuteDeleteAsync();
                await context.EPCISEvents.Where(x => insertedEventIds.Contains(x.EventId)).ExecuteDeleteAsync();
            }
        }

        /// <summary>
        /// Puts the event's search rows back into the legacy, not-yet-backfilled state.
        /// </summary>
        private async Task SetBizLocationToNullAsync(string eventId)
        {
            using ApplicationDbContext context = await _contextFactory.CreateDbContextAsync();
            await context.EventSearchDocuments.Where(x => x.EventId == eventId).ExecuteUpdateAsync(s => s.SetProperty(x => x.BizLocationGLN, (string?)null));
        }

        /// <summary>
        /// Asserts that every search row of the event carries the business location, that no NULL rows remain, and that the query finds it.
        /// </summary>
        private async Task AssertBackfilledAsync(string eventId, string expectedGLN)
        {
            using ApplicationDbContext context = await _contextFactory.CreateDbContextAsync();
            List<string?> values = await context.EventSearchDocuments.Where(x => x.EventId == eventId).Select(x => x.BizLocationGLN).ToListAsync();
            Assert.That(values, Is.Not.Empty);
            Assert.That(values, Is.All.EqualTo(expectedGLN.ToLower()), "Every search row of the event should carry its business location.");

            int remainingLegacy = await context.EventSearchDocuments.CountAsync(x => x.BizLocationGLN == null);
            Assert.That(remainingLegacy, Is.EqualTo(0), "No search rows should be left with a NULL BizLocationGLN.");

            EPCISQueryDocument result = await _dbService.QueryEvents(new EPCISQueryParameters { query = new EPCISQuery { EQ_bizLocation = new List<Uri> { new Uri(expectedGLN) } } });
            Assert.That(result.Events.Select(e => e.EventID?.ToString()), Is.EquivalentTo(new[] { eventId }));
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

        [Test]
        public async Task StoreEvents_ShouldSavePerformantly()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Load test data from JSON file
            string testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "testdata001.json");
            if (!File.Exists(testDataPath))
            {
                Assert.Fail($"Test data file not found at {testDataPath}");
            }

            string jsonData = File.ReadAllText(testDataPath);

            int maxEvents = 1000;
            int maxIterations = 1000;
            int iterations = 0;
            List<IEvent> events = new List<IEvent>();
            Random random = new();
            while (events.Count < maxEvents)
            {
                EPCISDocument doc = OpenTraceabilityMappers.EPCISDocument.JSON.Map(jsonData);

                foreach (var eventItem in doc.Events)
                {
                    foreach (var product in eventItem.Products)
                    {
                        if(product.EPC.Type == EPCType.SSCC)
                        {
                            List<string> epcParts = product.EPC.ToString().Split(":").ToList();
                            string lotOrSerial = epcParts.Last();
                            epcParts.Remove(lotOrSerial);
                            epcParts.Add(Guid.NewGuid().ToString());
                            string newEPC = string.Join(":", epcParts);
                            product.EPC = new EPC(newEPC);
                        }
                        else
                        {
                            product.EPC = new EPC(product.EPC.Type, product.EPC.GTIN, Guid.NewGuid().ToString());
                        }
                    }

                    eventItem.EventTime = eventItem.EventTime!.Value.AddMinutes(random.Next(0, 1000));
                    eventItem.EventID = new Uri("urn:uuid:" + Guid.NewGuid().ToString());
                    events.Add(eventItem);
                }

                if (events.Count >= maxEvents) break;

                iterations++;
                if(iterations >= maxIterations)
                {
                    Assert.Fail($"Failed to load {maxEvents} events after {maxIterations} iterations.");
                    break;
                }
            }

            // Measure the time taken to store events
            List<List<IEvent>> batches = events.Batch(100);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            foreach(var batch in batches)
            {
                await _dbService.StoreEventsAsync(batch);
            }
            stopwatch.Stop();
            TimeSpan savetime = stopwatch.Elapsed;

            stopwatch.Reset();

            // measure the time taken to update events
            stopwatch.Start();
            foreach (var batch in batches)
            {
                foreach (var eventItem in batch)
                {
                    eventItem.EventTime = eventItem.EventTime!.Value.AddMinutes(1);
                }
                await _dbService.StoreEventsAsync(batch);
            }
            stopwatch.Stop();
            TimeSpan updatetime = stopwatch.Elapsed;

            stopwatch.Reset();

            // Measure the time taken to query events
            stopwatch.Start();
            int eventCount = 0;
            foreach (var batch in batches.Take(1))
            {
                var result = await _dbService.QueryEvents(new EPCISQueryParameters
                {
                    query = new EPCISQuery
                    {
                        MATCH_anyEPCClass = batch.SelectMany(e => e.Products).Select(p => p.EPC.ToString()).Distinct().ToList()
                    }
                });
                eventCount += result.Events.Count;
            }
            stopwatch.Stop();
            TimeSpan querytime = stopwatch.Elapsed;

            stopwatch.Reset();

            Console.WriteLine($"Save Time: {savetime.TotalSeconds} s");
            Console.WriteLine($"Update Time: {updatetime.TotalSeconds} s");
            Console.WriteLine($"Query Time: {querytime.TotalSeconds} s");
        }
    }
}
