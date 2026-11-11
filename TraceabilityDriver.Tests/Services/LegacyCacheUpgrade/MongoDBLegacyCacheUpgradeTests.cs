using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services.LegacyCacheUpgrade
{
    /// <summary>
    /// Upgrade tests for <see cref="MongoDBService"/> against a cache written by the released v1.0.1 driver.
    /// </summary>
    /// <remarks>
    /// The snapshot is restored into its own database once per fixture, then upgraded by <see cref="MongoDBService.InitializeDatabase"/>.
    /// The pre-bizLocation build on main writes byte-identical event documents (apart from ids and timestamps), so this snapshot
    /// covers deployments on either version.
    /// </remarks>
    [TestFixture]
    [Category("AdvancedTest")]
    public class MongoDBLegacyCacheUpgradeTests
    {
        private const string DatabaseName = "TraceabilityDriverLegacyUpgradeTests";
        private const string SnapshotFile = "MongoEvents_v1_0_1.json";

        private bool _skipTests;
        private MongoClient _client = null!;
        private IMongoCollection<BsonDocument> _rawEvents = null!;
        private MongoDBService _service = null!;
        private List<BsonDocument> _legacyDocuments = null!;
        private EPCISQueryDocument _storedEvents = null!;
        private Dictionary<string, List<string>> _resultsBeforeUpgrade = null!;

        /// <summary>
        /// Restores the legacy snapshot, records non-location query results, then runs the startup upgrade once.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _skipTests = (Environment.GetEnvironmentVariable("NO_MONGO_DB") ?? string.Empty).Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            if (_skipTests)
            {
                Assert.Ignore("MongoDB tests skipped due to NO_MONGO_DB environment variable set to TRUE");
            }

            OpenTraceability.GDST.Setup.Initialize();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Tests.json")
                .AddInMemoryCollection(new Dictionary<string, string?> { ["MongoDB:DatabaseName"] = DatabaseName })
                .Build();

            // Restore the snapshot exactly as v1.0.1 wrote it, before the service touches the database.
            _client = new MongoClient(configuration["MongoDB:ConnectionString"]);
            await _client.DropDatabaseAsync(DatabaseName);
            _rawEvents = _client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(configuration["MongoDB:EventsCollectionName"]);
            _legacyDocuments = BsonSerializer.Deserialize<BsonArray>(LegacyCacheData.ReadFile(SnapshotFile)).Select(v => v.AsBsonDocument).ToList();
            await _rawEvents.InsertManyAsync(_legacyDocuments.Select(d => d.DeepClone().AsBsonDocument));

            _storedEvents = LegacyCacheData.ParseStoredEvents(_legacyDocuments.Select(d => d["EventJson"].AsString));
            _service = new MongoDBService(configuration, NullLogger<MongoDBService>.Instance);
            _resultsBeforeUpgrade = await LegacyCacheData.CaptureResultsAsync(_service, LegacyCacheData.BuildNonLocationQueries(_storedEvents));

            await _service.InitializeDatabase();
        }

        /// <summary>
        /// Drops the database restored for this fixture and releases the client.
        /// </summary>
        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (!_skipTests)
            {
                await _client.DropDatabaseAsync(DatabaseName);
                _client.Dispose();
            }
        }

        /// <summary>
        /// The snapshot must contain GLNs where legacy broad matching was wrong, otherwise these tests could not detect the bug.
        /// </summary>
        [Test]
        public void LegacySnapshot_BroadLocationMatching_DiffersFromLibraryFilter()
        {
            // Arrange
            Dictionary<string, List<string>> legacyLocationGLNs = _legacyDocuments.ToDictionary(d => d["EventId"].AsString, d => d["LocationGLNs"].AsBsonArray.Select(v => v.AsString.ToLower()).ToList());

            // Act
            int differingGLNs = LegacyCacheData.CountGLNsWhereLegacyMatchingDiffers(_storedEvents, legacyLocationGLNs);

            // Assert
            Assert.That(differingGLNs, Is.GreaterThan(0), "The snapshot should include GLNs used as source/destination locations of events at other bizLocations.");
        }

        /// <summary>
        /// Every legacy document receives the business location parsed from its own stored JSON.
        /// </summary>
        [Test]
        public async Task InitializeDatabase_LegacyCache_SetsBizLocationOnEveryDocument()
        {
            // Arrange
            Dictionary<string, string?> expectedByEventId = _storedEvents.Events.ToDictionary(e => e.EventID!.ToString(), e => e.Location?.GLN?.ToString().ToLower());

            // Act
            List<BsonDocument> upgraded = await _rawEvents.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

            // Assert
            Assert.That(upgraded, Has.Count.EqualTo(_legacyDocuments.Count));
            foreach (BsonDocument document in upgraded)
            {
                string eventId = document["EventId"].AsString;
                Assert.That(document.Contains("BizLocationGLN"), Is.True, $"Event {eventId} was not backfilled.");
                string? actual = document["BizLocationGLN"].IsBsonNull ? null : document["BizLocationGLN"].AsString;
                Assert.That(actual, Is.EqualTo(expectedByEventId[eventId]), $"Event {eventId} has the wrong bizLocation.");
            }
        }

        /// <summary>
        /// After the upgrade, EQ_bizLocation agrees with OpenTraceability's own filter for every GLN in the cache.
        /// </summary>
        [Test]
        public async Task QueryEvents_EveryGLNAfterUpgrade_MatchesLibraryFilter()
        {
            // Act
            List<string> mismatches = await LegacyCacheData.FindBizLocationMismatchesAsync(_service, _storedEvents);

            // Assert
            Assert.That(mismatches, Is.Empty, string.Join(Environment.NewLine, mismatches));
        }

        /// <summary>
        /// The upgrade must not change the results of any query that does not filter on location.
        /// </summary>
        [Test]
        public async Task QueryEvents_NonLocationFilters_UnchangedByUpgrade()
        {
            // Act
            Dictionary<string, List<string>> resultsAfterUpgrade = await LegacyCacheData.CaptureResultsAsync(_service, LegacyCacheData.BuildNonLocationQueries(_storedEvents));

            // Assert
            Assert.That(_resultsBeforeUpgrade.Values.Sum(ids => ids.Count), Is.GreaterThan(0), "The comparison queries should return events, or the check is vacuous.");
            Assert.That(resultsAfterUpgrade, Is.EqualTo(_resultsBeforeUpgrade));
        }

        /// <summary>
        /// Running the startup upgrade again changes nothing.
        /// </summary>
        [Test]
        public async Task InitializeDatabase_RunAgain_ChangesNothing()
        {
            // Arrange
            List<BsonDocument> before = await _rawEvents.Find(FilterDefinition<BsonDocument>.Empty).Sort(Builders<BsonDocument>.Sort.Ascending("EventId")).ToListAsync();

            // Act
            await _service.InitializeDatabase();

            // Assert
            List<BsonDocument> after = await _rawEvents.Find(FilterDefinition<BsonDocument>.Empty).Sort(Builders<BsonDocument>.Sort.Ascending("EventId")).ToListAsync();
            Assert.That(after, Is.EqualTo(before));
        }
    }
}
