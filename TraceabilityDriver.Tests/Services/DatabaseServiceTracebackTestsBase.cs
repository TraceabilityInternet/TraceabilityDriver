using Microsoft.Extensions.Configuration;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using OpenTraceability.Utility;
using TraceabilityDriver.Models.DB;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Models.Traceback;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Shared traceback storage tests that must hold for every <see cref="IDatabaseService"/> backend.
    /// </summary>
    /// <remarks>
    /// Both backends must satisfy the same contract: synced events are upserted by (event key, deployment
    /// version) with their EventID replaced by the CBV 2.0 content hash, traceback data is stored with a
    /// null deployment version and skipped when a record with its id is already in the cache under any
    /// deployment version, traceback records upsert by id, ledger items upsert by their
    /// (TracebackId, ItemType, ItemId) key, and the history queries order and filter correctly. Queries
    /// serve the synced data of the configured deployment version merged with the traceback data, with
    /// the synced copy winning on the same event id. Concrete fixtures supply the backend. Tests share
    /// one cleared database per fixture, so each test uses its own slice of the shared test document and
    /// asserts by event/element id rather than result counts.
    /// </remarks>
    public abstract class DatabaseServiceTracebackTestsBase
    {
        /// <summary>
        /// The deployment version configured in appsettings.Tests.json, which the backends under test
        /// read for their query paths.
        /// </summary>
        protected const string TestDeploymentVersion = "tests";

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
        /// The first store of an event must report it as created; storing the same events under the same
        /// keys again must report them as updated. The reported ids are the generated content-hash event
        /// ids, not the incoming event keys.
        /// </summary>
        [Test]
        public async Task StoreEventsAsync_StoredTwice_ReportsCreatedThenUpdated()
        {
            SkipIfUnavailable();

            // Arrange - the incoming EventID acts as the event key and is replaced by the store, so it
            // must be reset between stores the way the converter would stamp it on a resync.
            var events = _testDocument.Events.GroupBy(e => e.EventID.ToString()).Select(g => g.First()).Take(3).ToList();
            List<Uri> eventKeys = events.Select(e => e.EventID).ToList();

            // Act
            DatabaseStoreResult firstResult = await _dbService.StoreEventsAsync(events, TestDeploymentVersion);
            List<string> hashEventIds = events.Select(e => e.EventID.ToString()).OrderBy(x => x).ToList();

            for (int i = 0; i < events.Count; i++)
            {
                events[i].EventID = eventKeys[i];
            }
            DatabaseStoreResult secondResult = await _dbService.StoreEventsAsync(events, TestDeploymentVersion);

            // Assert
            Assert.That(hashEventIds.All(id => System.Text.RegularExpressions.Regex.IsMatch(id, @"^ni:///sha-256;[0-9a-f]{64}\?ver=CBV2\.0$")), Is.True, "The store must replace the incoming event key with the generated CBV 2.0 event hash.");
            Assert.That(firstResult.CreatedIds.OrderBy(x => x), Is.EqualTo(hashEventIds), "The first store must report every event as created.");
            Assert.That(firstResult.UpdatedIds, Is.Empty);
            Assert.That(secondResult.UpdatedIds.OrderBy(x => x), Is.EqualTo(hashEventIds), "The second store must report every event as updated.");
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
            DatabaseStoreResult firstResult = await _dbService.StoreMasterDataAsync(masterData, TestDeploymentVersion);
            DatabaseStoreResult secondResult = await _dbService.StoreMasterDataAsync(masterData, TestDeploymentVersion);

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

        /// <summary>
        /// Returns the distinct events of the shared test document that carry at least one product EPC,
        /// so tests can slice non-overlapping events and query them back by EPC.
        /// </summary>
        private List<IEvent> GetQueryableEvents()
        {
            return _testDocument.Events.Where(e => e.Products.Any()).GroupBy(e => e.EventID.ToString()).Select(g => g.First()).ToList();
        }

        /// <summary>
        /// Queries events matching the first product EPC of the given event.
        /// </summary>
        private async Task<EPCISQueryDocument> QueryEventsByEpcAsync(IEvent evt)
        {
            EPCISQueryParameters queryParameters = new EPCISQueryParameters
            {
                query = new EPCISQuery
                {
                    MATCH_anyEPC = new List<string> { evt.Products.First().EPC.ToString().ToLower() }
                }
            };

            return await _dbService.QueryEvents(queryParameters);
        }

        /// <summary>
        /// The same event stored under two deployment versions must coexist, and queries must serve only
        /// the copy of the currently configured deployment version.
        /// </summary>
        [Test]
        public async Task StoreEventsAsync_TwoDeploymentVersions_QueryServesOnlyCurrentVersion()
        {
            SkipIfUnavailable();

            // Arrange
            List<IEvent> events = GetQueryableEvents();
            IEvent currentVersionEvent = events[3];
            IEvent oldVersionOnlyEvent = events[9];

            // Act - the same event under an old version and the current version, plus an event that only
            // exists under the old version. The event key is reset between stores because the store
            // replaces the incoming EventID with the content hash.
            Uri currentVersionEventKey = currentVersionEvent.EventID;
            await _dbService.StoreEventsAsync(new List<IEvent> { currentVersionEvent }, "old-version");
            currentVersionEvent.EventID = currentVersionEventKey;
            await _dbService.StoreEventsAsync(new List<IEvent> { currentVersionEvent }, TestDeploymentVersion);
            await _dbService.StoreEventsAsync(new List<IEvent> { oldVersionOnlyEvent }, "old-version");

            EPCISQueryDocument currentResult = await QueryEventsByEpcAsync(currentVersionEvent);
            EPCISQueryDocument oldOnlyResult = await QueryEventsByEpcAsync(oldVersionOnlyEvent);

            // Assert
            Assert.That(currentResult.Events.Count(e => e.EventID == currentVersionEvent.EventID), Is.EqualTo(1), "The copies from both versions must coexist, but only the current version's copy is served.");
            Assert.That(oldOnlyResult.Events.Any(e => e.EventID == oldVersionOnlyEvent.EventID), Is.False, "An event synced only under an old deployment version must not be served.");
        }

        /// <summary>
        /// A tracebacked event whose event id already exists as a synced copy under the current
        /// deployment version must be skipped, and queries keep serving the synced copy; tracebacked
        /// events without a synced twin must always be served.
        /// </summary>
        [Test]
        public async Task StoreTracebackEventsAsync_SyncedCopyExists_SkipsTracebackCopy()
        {
            SkipIfUnavailable();

            // Arrange
            List<IEvent> events = GetQueryableEvents();
            IEvent overlappingEvent = events[4];
            IEvent tracebackOnlyEvent = events[5];
            DateTimeOffset syncedEventTime = overlappingEvent.EventTime!.Value;

            // Act - store the synced copy first (this replaces the EventID with the content hash), then
            // shift the event time so the traceback copy would be distinguishable if it were stored, and
            // store both events as traceback data.
            await _dbService.StoreEventsAsync(new List<IEvent> { overlappingEvent }, TestDeploymentVersion);
            overlappingEvent.EventTime = syncedEventTime.AddMinutes(5);
            DatabaseStoreResult tracebackResult = await _dbService.StoreTracebackEventsAsync(new List<IEvent> { overlappingEvent, tracebackOnlyEvent });

            EPCISQueryDocument overlapResult = await QueryEventsByEpcAsync(overlappingEvent);
            EPCISQueryDocument tracebackOnlyResult = await QueryEventsByEpcAsync(tracebackOnlyEvent);

            // Assert
            Assert.That(tracebackResult.CreatedIds, Is.EqualTo(new List<string> { tracebackOnlyEvent.EventID.ToString() }), "Only the event without a synced twin may be stored.");
            Assert.That(tracebackResult.UpdatedIds, Is.Empty, "Traceback data must never update existing records.");

            List<IEvent> overlappingCopies = overlapResult.Events.Where(e => e.EventID == overlappingEvent.EventID).ToList();
            Assert.That(overlappingCopies, Has.Count.EqualTo(1), "The overlapping event must be served exactly once.");
            Assert.That(overlappingCopies[0].EventTime, Is.EqualTo(syncedEventTime), "The synced copy must win over the tracebacked copy.");
            Assert.That(tracebackOnlyResult.Events.Any(e => e.EventID == tracebackOnlyEvent.EventID), Is.True, "Tracebacked events must always be served.");
        }

        /// <summary>
        /// When a traceback copy is stored first and the same event id is later synced under the current
        /// deployment version, both rows coexist but queries must serve only the synced copy.
        /// </summary>
        [Test]
        public async Task QueryEvents_TracebackStoredBeforeSync_SyncedCopyWins()
        {
            SkipIfUnavailable();

            // Arrange - give the traceback copy the exact event id the synced copy will be stored under
            // (the content hash of the unmodified event), then make its content distinguishable.
            List<IEvent> events = GetQueryableEvents();
            IEvent evt = events[10];
            Uri eventKey = evt.EventID;
            DateTimeOffset syncedEventTime = evt.EventTime!.Value;
            string expectedEventId = EventHashGenerator.GenerateHash(evt);

            evt.EventID = new Uri(expectedEventId);
            evt.EventTime = syncedEventTime.AddMinutes(5);
            await _dbService.StoreTracebackEventsAsync(new List<IEvent> { evt });

            // Act - restore the content and sync the event under the current deployment version.
            evt.EventTime = syncedEventTime;
            evt.EventID = eventKey;
            await _dbService.StoreEventsAsync(new List<IEvent> { evt }, TestDeploymentVersion);

            EPCISQueryDocument result = await QueryEventsByEpcAsync(evt);

            // Assert
            Assert.That(evt.EventID.ToString(), Is.EqualTo(expectedEventId), "The synced copy must be stored under the content hash the traceback copy carried.");
            List<IEvent> copies = result.Events.Where(e => e.EventID == evt.EventID).ToList();
            Assert.That(copies, Has.Count.EqualTo(1), "The same event id in both stores must be served once.");
            Assert.That(copies[0].EventTime, Is.EqualTo(syncedEventTime), "The synced copy must win over the tracebacked copy.");
        }

        /// <summary>
        /// The first traceback store of an event must report it as created; the second store must skip
        /// it entirely, reporting it as neither created nor updated.
        /// </summary>
        [Test]
        public async Task StoreTracebackEventsAsync_StoredTwice_ReportsCreatedThenSkips()
        {
            SkipIfUnavailable();

            // Arrange
            var events = GetQueryableEvents().Skip(6).Take(3).ToList();
            List<string> eventIds = events.Select(e => e.EventID.ToString()).OrderBy(x => x).ToList();

            // Act
            DatabaseStoreResult firstResult = await _dbService.StoreTracebackEventsAsync(events);
            DatabaseStoreResult secondResult = await _dbService.StoreTracebackEventsAsync(events);

            // Assert
            Assert.That(firstResult.CreatedIds.OrderBy(x => x), Is.EqualTo(eventIds), "The first store must report every event as created.");
            Assert.That(firstResult.UpdatedIds, Is.Empty);
            Assert.That(secondResult.CreatedIds, Is.Empty, "The second store must skip every event that already exists.");
            Assert.That(secondResult.UpdatedIds, Is.Empty, "Traceback data must never update existing records.");
        }

        /// <summary>
        /// An event that exists only under an old deployment version must still block a traceback store,
        /// because the incoming copy is superseded data that was pulled out of this cache in the first
        /// place and tracebacked back to us through another solution.
        /// </summary>
        [Test]
        public async Task StoreTracebackEventsAsync_ExistsOnlyUnderOldVersion_SkipsEvent()
        {
            SkipIfUnavailable();

            // Arrange - sync the event under an old version; the store leaves its EventID set to the
            // content hash, which is the id the traceback copy is then checked by.
            List<IEvent> events = GetQueryableEvents();
            IEvent evt = events[11];
            await _dbService.StoreEventsAsync(new List<IEvent> { evt }, "old-version");

            // Act
            DatabaseStoreResult result = await _dbService.StoreTracebackEventsAsync(new List<IEvent> { evt });

            // Assert
            Assert.That(result.CreatedIds, Is.Empty, "Any record with the same event id must block the traceback store, whatever deployment version it was stored under.");
            Assert.That(result.UpdatedIds, Is.Empty, "Traceback data must never update existing records.");
        }

        /// <summary>
        /// The first traceback store of a master data element must report it as created; the second store
        /// must skip it entirely, reporting it as neither created nor updated.
        /// </summary>
        [Test]
        public async Task StoreTracebackMasterDataAsync_StoredTwice_ReportsCreatedThenSkips()
        {
            SkipIfUnavailable();

            // Arrange
            var masterData = _testDocument.MasterData.GroupBy(m => m.ID).Select(g => g.First()).Skip(3).Take(3).ToList();
            List<string> elementIds = masterData.Select(m => m.ID).OrderBy(x => x).ToList();

            // Act
            DatabaseStoreResult firstResult = await _dbService.StoreTracebackMasterDataAsync(masterData);
            DatabaseStoreResult secondResult = await _dbService.StoreTracebackMasterDataAsync(masterData);

            // Assert
            Assert.That(firstResult.CreatedIds.OrderBy(x => x), Is.EqualTo(elementIds));
            Assert.That(firstResult.UpdatedIds, Is.Empty);
            Assert.That(secondResult.CreatedIds, Is.Empty, "The second store must skip every element that already exists.");
            Assert.That(secondResult.UpdatedIds, Is.Empty, "Traceback data must never update existing records.");
        }

        /// <summary>
        /// An element that exists only under an old deployment version must still block a traceback store,
        /// for the same reason as its event counterpart.
        /// </summary>
        [Test]
        public async Task StoreTracebackMasterDataAsync_ExistsOnlyUnderOldVersion_SkipsElement()
        {
            SkipIfUnavailable();

            // Arrange
            var masterData = _testDocument.MasterData.GroupBy(m => m.ID).Select(g => g.First()).ToList();
            IVocabularyElement element = masterData[8];
            await _dbService.StoreMasterDataAsync(new List<IVocabularyElement> { element }, "old-version");

            // Act
            DatabaseStoreResult result = await _dbService.StoreTracebackMasterDataAsync(new List<IVocabularyElement> { element });

            // Assert
            Assert.That(result.CreatedIds, Is.Empty, "Any record with the same element id must block the traceback store, whatever deployment version it was stored under.");
            Assert.That(result.UpdatedIds, Is.Empty, "Traceback data must never update existing records.");
        }

        /// <summary>
        /// Master data must fall back to the traceback store when the element was not synced under the
        /// current deployment version, and elements synced only under old versions must not be served.
        /// </summary>
        [Test]
        public async Task QueryMasterData_NotSyncedUnderCurrentVersion_FallsBackToTracebackStore()
        {
            SkipIfUnavailable();

            // Arrange
            var masterData = _testDocument.MasterData.GroupBy(m => m.ID).Select(g => g.First()).ToList();
            var tracebackOnlyElement = masterData[6];
            var oldVersionOnlyElement = masterData[7];

            // Act
            await _dbService.StoreTracebackMasterDataAsync(new List<IVocabularyElement> { tracebackOnlyElement });
            await _dbService.StoreMasterDataAsync(new List<IVocabularyElement> { oldVersionOnlyElement }, "old-version");

            IVocabularyElement? tracebackResult = await _dbService.QueryMasterData(tracebackOnlyElement.ID);
            IVocabularyElement? oldVersionResult = await _dbService.QueryMasterData(oldVersionOnlyElement.ID);

            // Assert
            Assert.That(tracebackResult, Is.Not.Null, "Tracebacked master data must be served when there is no synced copy.");
            Assert.That(tracebackResult!.ID, Is.EqualTo(tracebackOnlyElement.ID));
            Assert.That(oldVersionResult, Is.Null, "Master data synced only under an old deployment version must not be served.");
        }

        /// <summary>
        /// Re-storing an event under the same event key with additional content must merge into the existing
        /// record in place: the stored event picks up the new data, its event id switches to the merged
        /// content hash, and queries serve the merged copy exactly once with no stale copy left behind.
        /// </summary>
        /// <remarks>
        /// This is the straddle case the merge exists for: the rows of one event span sync runs, so a later
        /// partial copy has to enrich the stored event rather than replace it.
        /// </remarks>
        [Test]
        public async Task StoreEventsAsync_AdditionalContentUnderSameEventKey_MergesAndReplacesEventId()
        {
            SkipIfUnavailable();

            // Arrange
            IEvent evt = GetQueryableEvents().First(e => string.IsNullOrEmpty(e.CertificationInfo));
            Uri eventKey = evt.EventID;

            // Act - store, then store again under the same event key carrying a KDE the first copy lacked.
            await _dbService.StoreEventsAsync(new List<IEvent> { evt }, TestDeploymentVersion);
            string firstEventId = evt.EventID.ToString();

            evt.CertificationInfo = "https://example.org/certification/straddle-001";
            evt.EventID = eventKey;
            DatabaseStoreResult secondResult = await _dbService.StoreEventsAsync(new List<IEvent> { evt }, TestDeploymentVersion);
            string secondEventId = evt.EventID.ToString();

            EPCISQueryDocument result = await QueryEventsByEpcAsync(evt);

            // Assert
            Assert.That(secondEventId, Is.Not.EqualTo(firstEventId), "Merging in new content must change the content-hash event id.");
            Assert.That(secondResult.UpdatedIds, Is.EqualTo(new List<string> { secondEventId }), "The second store must update the existing record because it shares the event key.");
            Assert.That(secondResult.CreatedIds, Is.Empty);
            Assert.That(result.Events.Count(e => e.EventID.ToString() == secondEventId), Is.EqualTo(1), "The merged event must be served exactly once.");
            Assert.That(result.Events.Any(e => e.EventID.ToString() == firstEventId), Is.False, "No stale copy may remain under the superseded event id.");
            Assert.That(result.Events.Single(e => e.EventID.ToString() == secondEventId).CertificationInfo, Is.EqualTo("https://example.org/certification/straddle-001"), "The KDE that only arrived in the second store must be merged into the stored event.");
        }

        /// <summary>
        /// Re-storing an event under the same event key with a conflicting value must keep the stored value,
        /// because it came from earlier rows.
        /// </summary>
        /// <remarks>
        /// Correcting data that has already been synced under a deployment version is done by bumping
        /// DEPLOYMENT_VERSION, which forces a full resync, not by re-syncing into the same version.
        /// </remarks>
        [Test]
        public async Task StoreEventsAsync_ConflictingContentUnderSameEventKey_KeepsTheStoredValue()
        {
            SkipIfUnavailable();

            // Arrange
            IEvent evt = GetQueryableEvents()[12];
            Uri eventKey = evt.EventID;
            DateTimeOffset originalEventTime = evt.EventTime!.Value;

            // Act - store, then store again under the same event key with a different event time.
            await _dbService.StoreEventsAsync(new List<IEvent> { evt }, TestDeploymentVersion);
            string firstEventId = evt.EventID.ToString();

            evt.EventTime = originalEventTime.AddMinutes(7);
            evt.EventID = eventKey;
            await _dbService.StoreEventsAsync(new List<IEvent> { evt }, TestDeploymentVersion);

            EPCISQueryDocument result = await QueryEventsByEpcAsync(evt);

            // Assert
            IEvent? storedEvent = result.Events.FirstOrDefault(e => e.EventID.ToString() == firstEventId);
            Assert.That(storedEvent, Is.Not.Null, "The stored event keeps its content hash because the conflicting value was not taken.");
            Assert.That(storedEvent!.EventTime, Is.EqualTo(originalEventTime), "A value that arrived in an earlier store must win the conflict.");
        }

        /// <summary>
        /// The previous-sync lookup must return the latest sync of the requested deployment version only,
        /// and null for a version that has never synced.
        /// </summary>
        [Test]
        public async Task GetLatestSyncAsync_TwoDeploymentVersions_FiltersByVersion()
        {
            SkipIfUnavailable();

            // Arrange
            DateTime baseTime = DateTime.UtcNow;
            SyncHistoryItem olderSyncVersionA = new SyncHistoryItem { DeploymentVersion = "version-a", EndTime = baseTime.AddMinutes(-30), Status = SyncStatus.Completed };
            SyncHistoryItem latestSyncVersionA = new SyncHistoryItem { DeploymentVersion = "version-a", EndTime = baseTime.AddMinutes(-10), Status = SyncStatus.Completed };
            SyncHistoryItem syncVersionB = new SyncHistoryItem { DeploymentVersion = "version-b", EndTime = baseTime.AddMinutes(-5), Status = SyncStatus.Completed };

            // Act
            await _dbService.StoreSyncHistory(olderSyncVersionA);
            await _dbService.StoreSyncHistory(latestSyncVersionA);
            await _dbService.StoreSyncHistory(syncVersionB);

            SyncHistoryItem? latestA = await _dbService.GetLatestSyncAsync("version-a");
            SyncHistoryItem? latestB = await _dbService.GetLatestSyncAsync("version-b");
            SyncHistoryItem? unknown = await _dbService.GetLatestSyncAsync("version-that-never-synced");

            // Assert
            Assert.That(latestA, Is.Not.Null);
            Assert.That(latestA!.Id, Is.EqualTo(latestSyncVersionA.Id), "The lookup must return the latest sync of the requested version, not a newer sync of another version.");
            Assert.That(latestB!.Id, Is.EqualTo(syncVersionB.Id));
            Assert.That(unknown, Is.Null, "A deployment version that has never synced must return null so memory variables start from their defaults.");
        }
    }
}
