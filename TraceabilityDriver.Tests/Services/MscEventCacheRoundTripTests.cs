using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.MSC.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// The driver fills the transport KDEs, the product owner and the human welfare policy only on the
    /// MSC event classes — the GDST 2.0 classes have nowhere to put them. Those KDEs are exactly what
    /// MSC checks, so an MSC event has to survive the round trip through the data cache with them
    /// intact. This fixture registers the MSC profiles, which is the EnableMSC=true configuration.
    /// </summary>
    [TestFixture]
    [Category("AdvancedTest")]
    public class MscEventCacheRoundTripTests
    {
        private const string DeploymentVersion = "tests";
        private const string LotEpc = "urn:gdst:example.org:product:lot:class:msctest.tuna.lot1";
        private const string OwnerPgln = "urn:gdst:example.org:party:msctest.processor";
        private const string PlantGln = "urn:gdst:example.org:location:loc:msctest.plant1";
        private const string VehicleId = "urn:gdst:example.org:location:loc:msctest.truck1";

        private IDatabaseService _dbService = null!;
        private bool _skipTests;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _skipTests = (Environment.GetEnvironmentVariable("NO_SQL_DB") ?? string.Empty)
                .Equals("TRUE", StringComparison.OrdinalIgnoreCase);

            if (_skipTests)
            {
                Assert.Ignore("Tests skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Register the MSC profiles instead of the GDST ones. Registration is global, and every
            // other fixture re-registers its own in its setup, so this only affects this fixture.
            /// The control matters: with the GDST profiles registered instead, reading the same stored event
            /// throws "The KDE has a namespace prefix, but there is no such namespace in the dictionary. gdst"
            /// the cache row becomes unreadable. So EnableMSC is not a feature toggle that can be flipped after
            /// a sync; it is bound to the data already in the cache.
            OpenTraceability.MSC.Setup.Initialize();

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            string connectionString = configuration["SqlServer:ConnectionString"]
                ?? throw new Exception("SqlServer connection string not configured");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString, x => x.EnableRetryOnFailure())
                .Options;

            _dbService = new SqlServerService(
                new LoggerFactory().CreateLogger<SqlServerService>(),
                new PooledDbContextFactory<ApplicationDbContext>(options),
                configuration);

            await _dbService.ClearDatabaseAsync();
            await _dbService.InitializeDatabase();
            await _dbService.StoreEventsAsync(new List<IEvent> { BuildShippingEvent() }, DeploymentVersion);
        }

        /// <summary>
        /// A shipping event carrying every KDE that lives only on the MSC class.
        /// </summary>
        private static MSCShippingEvent BuildShippingEvent()
        {
            // The incoming EventID is the event key; the store replaces it with the content hash.
            var evt = new MSCShippingEvent
            {
                EventID = new Uri("urn:uuid:11111111-1111-4111-8111-111111111111"),
                EventTime = DateTimeOffset.Parse("2026-05-01T10:00:00+00:00"),
                EventTimeZoneOffset = TimeSpan.Zero,
                Action = EventAction.OBSERVE,
                Location = new EventLocation { GLN = new GLN(PlantGln) },
                ProductOwner = new PGLN(OwnerPgln),
                HumanWelfarePolicy = "3p",
                TransportType = "SEA",
                TransportVehicleID = VehicleId,
                TransportNumber = "SHIP-0001",
                TransportProviderID = OwnerPgln,
            };

            evt.AddProduct(new EventProduct(new EPC(LotEpc)) { Type = EventProductType.Reference });
            return evt;
        }

        /// <summary>
        /// Querying the stored event back must return it as an MSC shipping event with every KDE it
        /// went in with. A GDST 2.0 shipping event has none of these properties, so a registration that
        /// resolved the business step to the GDST class would drop all six in silence.
        /// </summary>
        [Test]
        public async Task StoredMscShippingEvent_ComesBackWithItsMscKdes()
        {
            if (_skipTests)
            {
                Assert.Ignore("Tests skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Act
            EPCISQueryDocument result = await _dbService.QueryEvents(new EPCISQueryParameters
            {
                query = new EPCISQuery { MATCH_anyEPC = new List<string> { LotEpc } }
            });

            // Assert
            Assert.That(result.Events, Has.Count.EqualTo(1));

            var shipping = result.Events[0] as MSCShippingEvent;
            Assert.That(shipping, Is.Not.Null, "The cached event did not come back as an MSC shipping event.");

            Assert.That(shipping!.TransportType, Is.EqualTo("SEA"));
            Assert.That(shipping.TransportVehicleID, Is.EqualTo(VehicleId));
            Assert.That(shipping.TransportNumber, Is.EqualTo("SHIP-0001"));
            Assert.That(shipping.TransportProviderID, Is.EqualTo(OwnerPgln));
            Assert.That(shipping.ProductOwner!.ToString(), Is.EqualTo(OwnerPgln));
            Assert.That(shipping.HumanWelfarePolicy, Is.EqualTo("3p"));
        }
    }
}

