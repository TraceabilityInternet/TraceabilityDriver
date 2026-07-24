using Microsoft.Extensions.Configuration;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Models.Traceback;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Shared traceback storage tests that must hold for every <see cref="IDatabaseService"/> backend.
    /// </summary>
    /// <remarks>
    /// Both backends must satisfy the same contract: store methods report created versus updated ids,
    /// traceback records upsert by id, ledger items upsert by their (TracebackId, ItemType, ItemId) key,
    /// and the history queries order and filter correctly. Concrete fixtures supply the backend.
    /// </remarks>
    public abstract class DatabaseServiceTracebackTestsBase
    {
        protected IDatabaseService _dbService = null!;
        protected bool _skipTests = false;
        private EPCISDocument _testDocument = null!;

        /// <summary>
        /// The environment variable that, when TRUE, skips this backend's tests.
        /// </summary>
        protected abstract string SkipEnvironmentVariable { get; }

        /// <summary>
        /// Creates the backend under test from the test configuration.
        /// </summary>
        protected abstract IDatabaseService CreateService(IConfiguration configuration);

        /// <summary>
        /// Builds the backend once, clears it, and loads the shared test document into memory.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            string skipValue = Environment.GetEnvironmentVariable(SkipEnvironmentVariable) ?? string.Empty;
            _skipTests = skipValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

            if (_skipTests)
            {
                Assert.Ignore($"Tests skipped due to {SkipEnvironmentVariable} environment variable set to TRUE");
                return;
            }

            OpenTraceability.GDST.Setup.Initialize();

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            _dbService = CreateService(configuration);

            await _dbService.ClearDatabaseAsync();
            await _dbService.InitializeDatabase();

            string testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "testdata001.json");
            _testDocument = OpenTraceabilityMappers.EPCISDocument.JSON.Map(File.ReadAllText(testDataPath));
        }

        /// <summary>
        /// Skips the current test when the backend is unavailable.
        /// </summary>
        protected void SkipIfUnavailable()
        {
            if (_skipTests)
            {
                Assert.Ignore($"Test skipped due to {SkipEnvironmentVariable} environment variable set to TRUE");
            }
        }

        /// <summary>
        /// The first store of an event must report it as created; the second store must report it as updated.
        /// </summary>
        [Test]
        public async Task StoreEventsAsync_StoredTwice_ReportsCreatedThenUpdated()
        {
            SkipIfUnavailable();

            // Arrange
            var events = _testDocument.Events.GroupBy(e => e.EventID.ToString()).Select(g => g.First()).Take(3).ToList();
            List<string> eventIds = events.Select(e => e.EventID.ToString()).OrderBy(x => x).ToList();

            // Act
            DatabaseStoreResult firstResult = await _dbService.StoreEventsAsync(events);
            DatabaseStoreResult secondResult = await _dbService.StoreEventsAsync(events);

            // Assert
            Assert.That(firstResult.CreatedIds.OrderBy(x => x), Is.EqualTo(eventIds), "The first store must report every event as created.");
            Assert.That(firstResult.UpdatedIds, Is.Empty);
            Assert.That(secondResult.UpdatedIds.OrderBy(x => x), Is.EqualTo(eventIds), "The second store must report every event as updated.");
            Assert.That(secondResult.CreatedIds, Is.Empty);
        }

        /// <summary>
        /// The first store of a master data element must report it as created; the second store must report it as updated.
        /// </summary>
        [Test]
        public async Task StoreMasterDataAsync_StoredTwice_ReportsCreatedThenUpdated()
        {
            SkipIfUnavailable();

            // Arrange
            var masterData = _testDocument.MasterData.GroupBy(m => m.ID).Select(g => g.First()).Take(3).ToList();
            List<string> elementIds = masterData.Select(m => m.ID).OrderBy(x => x).ToList();

            // Act
            DatabaseStoreResult firstResult = await _dbService.StoreMasterDataAsync(masterData);
            DatabaseStoreResult secondResult = await _dbService.StoreMasterDataAsync(masterData);

            // Assert
            Assert.That(firstResult.CreatedIds.OrderBy(x => x), Is.EqualTo(elementIds));
            Assert.That(firstResult.UpdatedIds, Is.Empty);
            Assert.That(secondResult.UpdatedIds.OrderBy(x => x), Is.EqualTo(elementIds));
            Assert.That(secondResult.CreatedIds, Is.Empty);
        }

        /// <summary>
        /// Storing the same traceback record twice (open, then finalize) must update it in place, not duplicate it.
        /// </summary>
        [Test]
        public async Task StoreTracebackAsync_StoredTwice_UpsertsById()
        {
            SkipIfUnavailable();

            // Arrange
            TracebackRecord record = new TracebackRecord { StartTime = DateTime.UtcNow, ResolverUrl = "https://resolver.example.com/", RequestedEpcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" } };

            // Act
            await _dbService.StoreTracebackAsync(record);
            record.Status = TracebackStatus.Completed;
            record.EndTime = DateTime.UtcNow;
            record.EventsCreated = 5;
            await _dbService.StoreTracebackAsync(record);

            // Assert
            TracebackRecord? stored = await _dbService.GetTracebackAsync(record.Id);
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Status, Is.EqualTo(TracebackStatus.Completed));
            Assert.That(stored.EventsCreated, Is.EqualTo(5));
            Assert.That(stored.EndTime, Is.Not.Null);
            Assert.That(stored.RequestedEpcs, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Rerunning a ledger write with the same (TracebackId, ItemType, ItemId) must not duplicate entries.
        /// </summary>
        [Test]
        public async Task StoreTracebackItemsAsync_RerunSameTriple_DoesNotDuplicate()
        {
            SkipIfUnavailable();

            // Arrange
            TracebackRecord record = new TracebackRecord { StartTime = DateTime.UtcNow, ResolverUrl = "https://resolver.example.com/" };
            await _dbService.StoreTracebackAsync(record);

            List<TracebackItem> items = new List<TracebackItem>
            {
                new TracebackItem { TracebackId = record.Id, ItemType = TracebackItemType.Event, ItemId = "event-idempotency-001", Created = true },
                new TracebackItem { TracebackId = record.Id, ItemType = TracebackItemType.MasterData, ItemId = "masterdata-idempotency-001", Created = true }
            };

            // Act
            await _dbService.StoreTracebackItemsAsync(items);

            // A retried run reports the same resources as updated rather than created.
            List<TracebackItem> retriedItems = new List<TracebackItem>
            {
                new TracebackItem { TracebackId = record.Id, ItemType = TracebackItemType.Event, ItemId = "event-idempotency-001", Created = false },
                new TracebackItem { TracebackId = record.Id, ItemType = TracebackItemType.MasterData, ItemId = "masterdata-idempotency-001", Created = false }
            };
            await _dbService.StoreTracebackItemsAsync(retriedItems);

            // Assert
            List<TracebackItem> stored = await _dbService.GetTracebackItemsAsync(record.Id);
            Assert.That(stored, Has.Count.EqualTo(2), "Retried ledger writes must upsert, not duplicate.");
            Assert.That(stored.All(i => !i.Created), Is.True, "The retried write must have replaced the Created flag.");
        }

        /// <summary>
        /// The traceback listing must be ordered newest first and honor top/skip paging.
        /// </summary>
        [Test]
        public async Task GetTracebacksAsync_MultipleRecords_OrdersNewestFirstAndPages()
        {
            SkipIfUnavailable();

            // Arrange
            DateTime baseTime = DateTime.UtcNow;
            TracebackRecord oldest = new TracebackRecord { StartTime = baseTime.AddMinutes(-30), ResolverUrl = "https://resolver.example.com/" };
            TracebackRecord middle = new TracebackRecord { StartTime = baseTime.AddMinutes(-20), ResolverUrl = "https://resolver.example.com/" };
            TracebackRecord newest = new TracebackRecord { StartTime = baseTime.AddMinutes(-10), ResolverUrl = "https://resolver.example.com/" };
            await _dbService.StoreTracebackAsync(oldest);
            await _dbService.StoreTracebackAsync(middle);
            await _dbService.StoreTracebackAsync(newest);

            // Act
            List<TracebackRecord> allRecords = await _dbService.GetTracebacksAsync(top: 1000, skip: 0);
            List<TracebackRecord> pagedRecords = await _dbService.GetTracebacksAsync(top: 2, skip: 1);

            // Assert
            List<string> myIdsInOrder = allRecords.Where(r => r.Id == oldest.Id || r.Id == middle.Id || r.Id == newest.Id).Select(r => r.Id).ToList();
            Assert.That(myIdsInOrder, Is.EqualTo(new List<string> { newest.Id, middle.Id, oldest.Id }), "Records must be ordered by start time descending.");

            Assert.That(pagedRecords, Has.Count.LessThanOrEqualTo(2));
            Assert.That(pagedRecords.Select(r => r.Id), Is.EqualTo(allRecords.Skip(1).Take(2).Select(r => r.Id)), "Paging must slice the same descending ordering.");
        }

        /// <summary>
        /// The ledger query must only return items belonging to the requested traceback.
        /// </summary>
        [Test]
        public async Task GetTracebackItemsAsync_TwoTracebacks_FiltersByTracebackId()
        {
            SkipIfUnavailable();

            // Arrange
            TracebackRecord recordA = new TracebackRecord { StartTime = DateTime.UtcNow, ResolverUrl = "https://resolver.example.com/" };
            TracebackRecord recordB = new TracebackRecord { StartTime = DateTime.UtcNow, ResolverUrl = "https://resolver.example.com/" };
            await _dbService.StoreTracebackAsync(recordA);
            await _dbService.StoreTracebackAsync(recordB);

            await _dbService.StoreTracebackItemsAsync(new List<TracebackItem>
            {
                new TracebackItem { TracebackId = recordA.Id, ItemType = TracebackItemType.Event, ItemId = "event-filter-A", Created = true },
                new TracebackItem { TracebackId = recordB.Id, ItemType = TracebackItemType.Event, ItemId = "event-filter-B", Created = true }
            });

            // Act
            List<TracebackItem> itemsA = await _dbService.GetTracebackItemsAsync(recordA.Id);

            // Assert
            Assert.That(itemsA, Has.Count.EqualTo(1));
            Assert.That(itemsA[0].ItemId, Is.EqualTo("event-filter-A"));
        }

        /// <summary>
        /// An unknown traceback id must return null rather than throw.
        /// </summary>
        [Test]
        public async Task GetTracebackAsync_UnknownId_ReturnsNull()
        {
            SkipIfUnavailable();

            // Act
            TracebackRecord? record = await _dbService.GetTracebackAsync(MongoDB.Bson.ObjectId.GenerateNewId().ToString());

            // Assert
            Assert.That(record, Is.Null);
        }
    }
}
