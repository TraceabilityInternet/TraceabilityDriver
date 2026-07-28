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
    /// Unit tests for the deployment version behavior of <see cref="SynchronizeService"/>.
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
            _mockDatabaseService.Verify(d => d.StoreEventsAsync(It.IsAny<List<IEvent>>(), It.IsAny<string>()), Times.Never);
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

    }
}
