using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTraceability.Interfaces;
using TraceabilityDriver.Models.DB;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.Mapping;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Unit tests for the deployment version behavior of <see cref="SynchronizeService"/>, including the
    /// version-scoped merge with previously stored events.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SynchronizeServiceDeploymentVersionTests
    {
        private Mock<ILogger<SynchronizeService>> _mockLogger = null!;
        private Mock<ITDConnectorFactory> _mockConnectorFactory = null!;
        private Mock<IEventsMergerService> _mockEventsMerger = null!;
        private Mock<IEventsConverterService> _mockEventsConverter = null!;
        private Mock<IDatabaseService> _mockDatabaseService = null!;
        private Mock<IMappingSource> _mockMappingSource = null!;
        private TraceabilityDriver.Services.SynchronizationContext _syncContext = null!;

        /// <summary>
        /// Builds fresh mocks with an empty mapping source before each test.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<SynchronizeService>>();
            _mockConnectorFactory = new Mock<ITDConnectorFactory>();
            _mockEventsMerger = new Mock<IEventsMergerService>();
            _mockEventsConverter = new Mock<IEventsConverterService>();
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockMappingSource = new Mock<IMappingSource>();
            _syncContext = new TraceabilityDriver.Services.SynchronizationContext();

            _mockMappingSource.Setup(m => m.GetMappings()).Returns(new List<TDMappingConfiguration>());
        }

        /// <summary>
        /// Builds the service under test with the given configuration values.
        /// </summary>
        private SynchronizeService CreateService(Dictionary<string, string?> configurationValues)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();

            return new SynchronizeService(
                _mockLogger.Object,
                _mockConnectorFactory.Object,
                _mockEventsMerger.Object,
                _mockEventsConverter.Object,
                _mockDatabaseService.Object,
                _mockMappingSource.Object,
                _syncContext,
                configuration);
        }

        /// <summary>
        /// Without a configured deployment version the sync must fail with a logged error and never touch the database.
        /// </summary>
        [Test]
        public async Task SynchronizeAsync_NoDeploymentVersion_FailsWithLoggedErrorAndNoDatabaseCalls()
        {
            // Arrange
            SynchronizeService service = CreateService(new Dictionary<string, string?>());

            // Act
            await service.SynchronizeAsync(CancellationToken.None);

            // Assert
            Assert.That(_syncContext.CurrentSync.Status, Is.EqualTo(SyncStatus.Failed));
            Assert.That(_syncContext.CurrentSync.EndTime, Is.Not.Null);
            Assert.That(_syncContext.CurrentSync.Message, Does.Contain("DEPLOYMENT_VERSION"));

            _mockLogger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DEPLOYMENT_VERSION")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

            _mockDatabaseService.Verify(d => d.GetLatestSyncAsync(It.IsAny<string>()), Times.Never);
            _mockDatabaseService.Verify(d => d.StoreEventsAsync(It.IsAny<List<IEvent>>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, CommonEvent>>()), Times.Never);
            _mockDatabaseService.Verify(d => d.StoreSyncHistory(It.IsAny<SyncHistoryItem>()), Times.Never, "The failure repeats every loop iteration and must not flood the sync history.");
        }

        /// <summary>
        /// With a configured deployment version the sync must load the previous sync of that version and
        /// stamp the version onto the stored sync history.
        /// </summary>
        [Test]
        public async Task SynchronizeAsync_WithDeploymentVersion_LoadsVersionScopedPreviousSyncAndStampsHistory()
        {
            // Arrange
            SyncHistoryItem? storedHistory = null;
            _mockDatabaseService.Setup(d => d.GetLatestSyncAsync("v1")).ReturnsAsync((SyncHistoryItem?)null);
            _mockDatabaseService.Setup(d => d.StoreSyncHistory(It.IsAny<SyncHistoryItem>())).Callback((SyncHistoryItem item) => storedHistory = item).Returns(Task.CompletedTask);

            SynchronizeService service = CreateService(new Dictionary<string, string?> { ["DEPLOYMENT_VERSION"] = "v1" });

            // Act
            await service.SynchronizeAsync(CancellationToken.None);

            // Assert
            Assert.That(_syncContext.CurrentSync.Status, Is.EqualTo(SyncStatus.Completed));
            Assert.That(_syncContext.CurrentSync.DeploymentVersion, Is.EqualTo("v1"));

            _mockDatabaseService.Verify(d => d.GetLatestSyncAsync("v1"), Times.Once, "The previous sync must be looked up by the configured deployment version.");

            Assert.That(storedHistory, Is.Not.Null);
            Assert.That(storedHistory!.DeploymentVersion, Is.EqualTo("v1"), "The stored sync history must carry the deployment version it ran under.");
        }

        /// <summary>
        /// When a partial copy of an event was stored by an earlier sync run, the incoming partial must
        /// be merged into it: stored values win conflicts, incoming values fill the gaps, and events
        /// without a stored counterpart pass through unchanged.
        /// </summary>
        [Test]
        public async Task MergeWithStoredEventsAsync_StoredPartialExists_MergesIncomingIntoStoredEvent()
        {
            // Arrange
            CommonEvent incoming = new CommonEvent { EventKey = "evt-1", EventType = "commissioningevent", TransportNumber = "TN-INCOMING", ProcessingType = "freezing" };
            CommonEvent unrelated = new CommonEvent { EventKey = "evt-2", EventType = "commissioningevent" };
            CommonEvent stored = new CommonEvent { EventKey = "evt-1", EventType = "commissioningevent", TransportNumber = "TN-STORED", EventTime = new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.Zero) };

            string storedEventKey = stored.GetEventKey().ToString();
            _mockDatabaseService.Setup(d => d.GetCommonEventsAsync(It.Is<List<string>>(keys => keys.Contains(storedEventKey)), "v1")).ReturnsAsync(new Dictionary<string, CommonEvent> { [storedEventKey] = stored });

            SynchronizeService service = CreateService(new Dictionary<string, string?> { ["DEPLOYMENT_VERSION"] = "v1" });

            // Act
            List<CommonEvent> result = await service.MergeWithStoredEventsAsync(new List<CommonEvent> { incoming, unrelated });

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0], Is.SameAs(stored), "The stored event must be the merge target.");
            Assert.That(result[0].TransportNumber, Is.EqualTo("TN-STORED"), "Stored values must win conflicts because they came from earlier rows.");
            Assert.That(result[0].ProcessingType, Is.EqualTo("freezing"), "Incoming values must fill the gaps in the stored event.");
            Assert.That(result[0].EventTime, Is.Not.Null);
            Assert.That(result[1], Is.SameAs(unrelated), "Events without a stored counterpart must pass through unchanged.");
        }

        /// <summary>
        /// When no events were previously stored under the deployment version, the incoming events must
        /// pass through unchanged.
        /// </summary>
        [Test]
        public async Task MergeWithStoredEventsAsync_NoStoredEvents_ReturnsIncomingEventsUnchanged()
        {
            // Arrange
            CommonEvent incoming = new CommonEvent { EventKey = "evt-1", EventType = "commissioningevent" };
            _mockDatabaseService.Setup(d => d.GetCommonEventsAsync(It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(new Dictionary<string, CommonEvent>());

            SynchronizeService service = CreateService(new Dictionary<string, string?> { ["DEPLOYMENT_VERSION"] = "v1" });

            // Act
            List<CommonEvent> result = await service.MergeWithStoredEventsAsync(new List<CommonEvent> { incoming });

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.SameAs(incoming));
        }
    }
}
