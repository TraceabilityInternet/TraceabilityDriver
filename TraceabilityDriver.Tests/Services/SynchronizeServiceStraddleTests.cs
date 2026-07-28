using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTraceability.GDST.Events;
using OpenTraceability.GDST.MasterData;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Models.DB;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.Mapping;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Tests that an event and its master data accumulate across sync runs instead of being replaced by the
    /// newest partial copy.
    /// </summary>
    /// <remarks>
    /// The connectors read a bounded number of rows per run, so the rows describing one event routinely
    /// straddle sync runs: the event time arrives in one run and the product owner in the next. The store
    /// upserts by (event key, deployment version), so without a merge the second run's partial copy replaces
    /// the first run's data and the cache ends up with an incomplete event.
    ///
    /// Only the source side is mocked - a stub <see cref="ITDConnector"/> hands out one half of the event per
    /// run. The merger, the converter, and the MongoDB data cache are all real, and the assertions read the
    /// event and the location back out of the database.
    /// </remarks>
    [TestFixture]
    [Category("AdvancedTest")]
    public class SynchronizeServiceStraddleTests
    {
        private const string DeploymentVersion = "tests";
        private const string EventKey = "straddle-evt-001";
        private const string ProductEpc = "urn:epc:id:sgtin:0614141.107346.2018";
        private const string ProductDefinitionGtin = "urn:epc:idpat:sgtin:0614141.107346";
        private const string LocationGln = "urn:epc:id:sgln:0614141.12345.0";
        private const string OwnerPgln = "urn:epc:id:pgln:0614141.12345";

        private IConfiguration _configuration = null!;
        private MongoDBService _dbService = null!;
        private List<CommonEvent> _eventsFromSource = new List<CommonEvent>();
        private bool _skipTests;

        /// <summary>
        /// Builds the MongoDB data cache once for the fixture.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            string skipValue = Environment.GetEnvironmentVariable("NO_MONGO_DB") ?? string.Empty;
            _skipTests = skipValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

            if (_skipTests)
            {
                Assert.Ignore("Tests skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            OpenTraceability.GDST.Setup.Initialize();

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            _dbService = new MongoDBService(new LoggerFactory().CreateLogger<MongoDBService>(), _configuration);
        }

        /// <summary>
        /// Clears the cache before each test so the two sync runs start from an empty database.
        /// </summary>
        [SetUp]
        public async Task SetUp()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            await _dbService.ClearDatabaseAsync();
            _eventsFromSource = new List<CommonEvent>();
        }

        /// <summary>
        /// An event whose event-level KDEs arrive in different sync runs must end up in the cache carrying
        /// both halves.
        /// </summary>
        [Test]
        public async Task SynchronizeAsync_EventStraddlesTwoSyncRuns_StoredEventCarriesBothHalves()
        {
            // Arrange - the first run carries the human welfare policy, the second carries the product owner.
            SynchronizeService service = CreateService();

            CommonEvent firstHalf = CreateCommonEvent();
            firstHalf.HumanWelfarePolicy = "https://example.org/human-welfare-policy";

            CommonEvent secondHalf = CreateCommonEvent();
            secondHalf.ProductOwner = new CommonParty { OwnerId = OwnerPgln, Name = "Acme Seafood" };

            // Act
            _eventsFromSource = new List<CommonEvent> { firstHalf };
            await service.SynchronizeAsync(CancellationToken.None);

            _eventsFromSource = new List<CommonEvent> { secondHalf };
            await service.SynchronizeAsync(CancellationToken.None);

            // Assert
            GDSTCommissionEvent storedEvent = await GetStoredCommissionEventAsync();

            Assert.That(storedEvent.ProductOwner, Is.Not.Null, "The product owner arrived in the second sync run and must be stored.");
            Assert.That(storedEvent.ProductOwner!.ToString(), Is.EqualTo(OwnerPgln));
            Assert.That(storedEvent.HumanWelfarePolicy, Is.EqualTo("https://example.org/human-welfare-policy"),
                "The human welfare policy arrived in the first sync run. The second run stored a partial copy of the same event key, which replaced it instead of enriching it.");
        }

        /// <summary>
        /// A location whose master data KDEs arrive in different sync runs must end up in the cache carrying
        /// both halves.
        /// </summary>
        [Test]
        public async Task SynchronizeAsync_MasterDataStraddlesTwoSyncRuns_StoredLocationCarriesBothHalves()
        {
            // Arrange - the first run carries the city, the second carries the vessel id.
            SynchronizeService service = CreateService();

            CommonEvent firstHalf = CreateCommonEvent();
            firstHalf.Location!.City = "Seattle";

            CommonEvent secondHalf = CreateCommonEvent();
            secondHalf.Location!.VesselId = "VESSEL-001";

            // Act
            _eventsFromSource = new List<CommonEvent> { firstHalf };
            await service.SynchronizeAsync(CancellationToken.None);

            _eventsFromSource = new List<CommonEvent> { secondHalf };
            await service.SynchronizeAsync(CancellationToken.None);

            // Assert
            IVocabularyElement? storedElement = await _dbService.QueryMasterData(LocationGln);
            Assert.That(storedElement, Is.InstanceOf<GDSTLocation>(), "The synced location must be served from the data cache.");

            GDSTLocation storedLocation = (GDSTLocation)storedElement!;

            Assert.That(storedLocation.VesselID, Is.EqualTo("VESSEL-001"), "The vessel id arrived in the second sync run and must be stored.");
            Assert.That(storedLocation.Address?.City?.FirstOrDefault()?.Value, Is.EqualTo("Seattle"),
                "The city arrived in the first sync run. The second run stored a partial copy of the same location, which replaced it instead of enriching it.");
        }

        /// <summary>
        /// Builds one half of the straddled event. Every half carries the identity, the event time, the
        /// product, and the location, because those are what the store matches on and what the converter
        /// requires; the halves differ only in the KDEs the test sets afterwards.
        /// </summary>
        private static CommonEvent CreateCommonEvent()
        {
            return new CommonEvent
            {
                EventKey = EventKey,
                EventType = "commissioningevent",
                EventTime = new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.Zero),
                Location = new CommonLocation
                {
                    LocationId = LocationGln,
                    Name = "Pier 42"
                },
                Products = new List<CommonProduct>
                {
                    new CommonProduct
                    {
                        ProductId = ProductEpc,
                        ProductType = OpenTraceability.Models.Events.EventProductType.Reference,
                        Quantity = 100.5,
                        UoM = "KGM",
                        ProductDefinition = new CommonProductDefinition
                        {
                            ProductDefinitionId = ProductDefinitionGtin,
                            ShortDescription = "Yellowfin Tuna"
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Reads the single synced commissioning event back out of the data cache by its product EPC.
        /// </summary>
        private async Task<GDSTCommissionEvent> GetStoredCommissionEventAsync()
        {
            EPCISQueryParameters queryParameters = new EPCISQueryParameters
            {
                query = new EPCISQuery
                {
                    MATCH_anyEPC = new List<string> { ProductEpc.ToLower() }
                }
            };

            EPCISQueryDocument result = await _dbService.QueryEvents(queryParameters);

            Assert.That(result.Events, Has.Count.EqualTo(1), "The two sync runs describe one event, so exactly one event must be stored.");
            Assert.That(result.Events.Single(), Is.InstanceOf<GDSTCommissionEvent>());

            return (GDSTCommissionEvent)result.Events.Single();
        }

        /// <summary>
        /// Builds the service under test over a stub source connector and the real merger, converter, and
        /// MongoDB data cache.
        /// </summary>
        private SynchronizeService CreateService()
        {
            Mock<ITDConnector> mockConnector = new Mock<ITDConnector>();
            mockConnector.Setup(c => c.TestConnectionAsync(It.IsAny<TDConnectorConfiguration>())).ReturnsAsync(true);
            mockConnector.Setup(c => c.GetTotalRowsAsync(It.IsAny<TDConnectorConfiguration>(), It.IsAny<TDMappingSelector>())).ReturnsAsync(() => _eventsFromSource.Count);
            mockConnector.Setup(c => c.GetEventsAsync(It.IsAny<TDConnectorConfiguration>(), It.IsAny<TDMappingSelector>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => _eventsFromSource);

            Mock<ITDConnectorFactory> mockConnectorFactory = new Mock<ITDConnectorFactory>();
            mockConnectorFactory.Setup(f => f.CreateConnector(It.IsAny<TDConnectorConfiguration>())).Returns(mockConnector.Object);

            Mock<IMappingSource> mockMappingSource = new Mock<IMappingSource>();
            mockMappingSource.Setup(m => m.GetMappings()).Returns(new List<TDMappingConfiguration> { CreateMappingConfiguration() });

            ILoggerFactory loggerFactory = new LoggerFactory();

            return new SynchronizeService(
                loggerFactory.CreateLogger<SynchronizeService>(),
                mockConnectorFactory.Object,
                new EventsMergeByIdService(loggerFactory.CreateLogger<EventsMergeByIdService>()),
                new EventsConverterService(loggerFactory.CreateLogger<EventsConverterService>()),
                _dbService,
                mockMappingSource.Object,
                new TraceabilityDriver.Services.SynchronizationContext(),
                _configuration);
        }

        /// <summary>
        /// Builds the minimal mapping configuration the sync needs. The stub connector ignores the selector,
        /// so only the connection name has to line up.
        /// </summary>
        private static TDMappingConfiguration CreateMappingConfiguration()
        {
            return new TDMappingConfiguration
            {
                Connections = new Dictionary<string, TDConnectorConfiguration>
                {
                    ["SOURCE_DB"] = new TDConnectorConfiguration { Database = "SOURCE_DB", ConnectorType = ConnectorType.SqlServer }
                },
                Mappings = new List<TDMapping>
                {
                    new TDMapping
                    {
                        EventType = "commissioningevent",
                        Selectors = new List<TDMappingSelector>
                        {
                            new TDMappingSelector { Database = "SOURCE_DB", EventMapping = new TDEventMapping() }
                        }
                    }
                }
            };
        }
    }
}
