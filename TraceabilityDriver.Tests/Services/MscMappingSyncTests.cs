using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.MSC.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.Mapping;
using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// A mapping asking for an MSC event must reach the MSC branch of the converter and carry the KDEs
    /// that live only on the MSC classes. Every sample mapping in the repository emits GDST event
    /// types, so this path had no coverage: a mapping could name an MSC event type and quietly produce
    /// a GDST event without its transport KDEs, which are four of the checks MSC runs on a shipment.
    /// </summary>
    /// <remarks>
    /// The fixture registers the MSC profiles, which is the EnableMSC=true configuration. It drives the
    /// real table mapping service and the real converter — the existing connector sync tests mock the
    /// table mapping service, so none of them exercises a mapping reaching the converter.
    /// </remarks>
    [TestFixture]
    [Category("AdvancedTest")]
    public class MscMappingSyncTests
    {
        private const string SourceDatabase = "TraceabilityDriverMscTestDB";
        private const string MasterConnectionString = "server=localhost;database=master;Integrated Security=SSPI;TrustServerCertificate=True;";
        private const string SourceConnectionString = "server=localhost;database=" + SourceDatabase + ";Integrated Security=SSPI;TrustServerCertificate=True;";

        private const string LotEpc = "urn:gdst:example.org:product:lot:class:mscmap.tuna.lot1";
        private const string ProductGtin = "urn:gdst:example.org:product:class:mscmap.tuna";
        private const string OwnerPgln = "urn:gdst:example.org:party:mscmap.processor";
        private const string PlantGln = "urn:gdst:example.org:location:loc:mscmap.plant1";
        private const string VehicleId = "urn:gdst:example.org:location:loc:mscmap.truck1";

        private IServiceProvider _services = null!;
        private TestLoggerProvider _loggerProvider = null!;
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

            // Register the MSC profiles instead of the GDST ones: this is the EnableMSC=true
            // configuration. Registration is global, and every other fixture re-registers its own in
            // its setup, so this only affects this fixture.
            OpenTraceability.MSC.Setup.Initialize();

            SetupSourceDatabase();

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            _services = BuildSyncServices(configuration);

            IDatabaseService dbService = _services.GetRequiredService<IDatabaseService>();
            await dbService.ClearDatabaseAsync();
            await dbService.InitializeDatabase();

            ISynchronizeService syncService = _services.GetRequiredService<ISynchronizeService>();
            await syncService.SynchronizeAsync(TestContext.CurrentContext.CancellationToken);
        }

        /// <summary>
        /// Creates the source database with one shipment row carrying the four transport KDEs, the
        /// product owner and the human welfare policy.
        /// </summary>
        private static void SetupSourceDatabase()
        {
            using (var connection = new SqlConnection(MasterConnectionString))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                        IF EXISTS(SELECT * FROM sys.databases WHERE name = '{SourceDatabase}')
                        BEGIN
                            ALTER DATABASE {SourceDatabase} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                            DROP DATABASE {SourceDatabase};
                        END
                        CREATE DATABASE {SourceDatabase};";
                cmd.ExecuteNonQuery();
            }

            using (var connection = new SqlConnection(SourceConnectionString))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                        CREATE TABLE Shipments (
                            Id                    int NOT NULL PRIMARY KEY,
                            EventTimeUtc          nvarchar(40) NOT NULL,
                            InfoPgln              nvarchar(200) NOT NULL,
                            OwnerPgln             nvarchar(200) NOT NULL,
                            LocationGln           nvarchar(200) NOT NULL,
                            HumanWelfarePolicy    nvarchar(40) NULL,
                            TransportType         nvarchar(20) NULL,
                            TransportVehicleId    nvarchar(200) NULL,
                            TransportNumber       nvarchar(100) NULL,
                            TransportProviderPgln nvarchar(200) NULL,
                            Epc                   nvarchar(200) NOT NULL,
                            Quantity              float NULL,
                            Uom                   nvarchar(10) NULL,
                            ProductGtin           nvarchar(200) NULL
                        );

                        INSERT INTO Shipments
                        VALUES (1, '2026-05-01T10:00:00.0000000+00:00', '{OwnerPgln}', '{OwnerPgln}',
                                '{PlantGln}', '3p', 'SEA', '{VehicleId}', 'SHIP-0001', '{OwnerPgln}',
                                '{LotEpc}', 9500, 'KGM', '{ProductGtin}');";
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Wires the real sync pipeline over the inline mapping and the test data cache.
        /// </summary>
        private IServiceProvider BuildSyncServices(IConfiguration configuration)
        {
            TDMappingConfiguration mapping = JsonConvert.DeserializeObject<TDMappingConfiguration>(MappingJson)!;
            var mappingSource = new Mock<IMappingSource>();
            mappingSource.Setup(x => x.GetMappings()).Returns(new List<TDMappingConfiguration> { mapping });

            _loggerProvider = new TestLoggerProvider();

            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddProvider(_loggerProvider);
            });
            services.AddSingleton(configuration);

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
            };

            string cacheConnectionString = configuration["SqlServer:ConnectionString"]
                ?? throw new InvalidOperationException("SqlServer:ConnectionString not found in appsettings.Tests.json");
            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseSqlServer(cacheConnectionString, sqlOpts => sqlOpts.EnableRetryOnFailure()));
            services.AddScoped<IDatabaseService, SqlServerService>();

            services.AddSingleton<ISynchronizeService, SynchronizeService>();
            services.AddSingleton<ITDConnectorFactory, TDConnectorFactory>();
            services.AddTransient<TDSqlServerConnector>();

            services.AddScoped<ISynchronizationContext, TraceabilityDriver.Services.SynchronizationContext>();
            services.AddSingleton<IMappingSource>(mappingSource.Object);
            services.AddTransient<IEventsTableMappingService, EventsTableMappingService>();
            services.AddTransient<IEventsConverterService, EventsConverterService>();
            services.AddTransient<IEventsMergerService, EventsMergeByIdService>();

            services.AddSingleton<IMappingFunctionFactory, MappingFunctionFactory>();
            services.AddKeyedTransient<IMappingFunction, DictionaryMappingFunction>("dictionary");
            services.AddKeyedTransient<IMappingFunction, GenerateIdentifierFunction>("generateidentifier");
            services.AddKeyedTransient<IMappingFunction, JoinFunction>("join");

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Syncing a mapping that asks for an MSC shipping event must put one in the cache with every
        /// KDE the source row supplied. A GDST shipping event has nowhere to hold the transport KDEs,
        /// the product owner or the welfare policy, so routing to the wrong branch loses all six.
        /// </summary>
        [Test]
        public async Task SyncedMscShippingMapping_ProducesAnMscEventWithItsKdes()
        {
            if (_skipTests)
            {
                Assert.Ignore("Tests skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Assert: the converter rejects an event it cannot build and only logs the reason, so a
            // silent rejection has to fail the test rather than show up as an empty query result.
            List<(LogLevel Level, string Message, Exception? Exception)> errors =
                _loggerProvider.LogEntries.Where(e => e.Level >= LogLevel.Error).ToList();
            Assert.That(errors, Is.Empty, "The sync logged errors:\n" + string.Join("\n", errors.Select(e => "  - " + e.Message)));

            // Act
            IDatabaseService dbService = _services.GetRequiredService<IDatabaseService>();
            EPCISQueryDocument result = await dbService.QueryEvents(new EPCISQueryParameters
            {
                query = new EPCISQuery { MATCH_anyEPC = new List<string> { LotEpc } }
            });

            // Assert
            Assert.That(result.Events, Has.Count.EqualTo(1));

            var shipping = result.Events[0] as MSCShippingEvent;
            Assert.That(shipping, Is.Not.Null, "The mapping asked for an MSC shipping event but the sync produced something else.");

            Assert.That(shipping!.TransportType, Is.EqualTo("SEA"));
            Assert.That(shipping.TransportVehicleID, Is.EqualTo(VehicleId));
            Assert.That(shipping.TransportNumber, Is.EqualTo("SHIP-0001"));
            Assert.That(shipping.TransportProviderID, Is.EqualTo(OwnerPgln));
            Assert.That(shipping.ProductOwner!.ToString(), Is.EqualTo(OwnerPgln));
            Assert.That(shipping.HumanWelfarePolicy, Is.EqualTo("3p"));
        }

        /// <summary>
        /// The EPCIS endpoint answers by running the queried document through the SDK mapper, so the
        /// MSC KDEs have to survive serialization and come back when the document is read again. This
        /// is the half MSC sees: the driver can hold the KDEs in its cache and still drop them on the
        /// wire, which is how the master data classifications were lost in the SDK before beta.118.
        /// </summary>
        [Test]
        public async Task ServedMscShippingEvent_KeepsItsKdesThroughTheEpcisMapper()
        {
            if (_skipTests)
            {
                Assert.Ignore("Tests skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            IDatabaseService dbService = _services.GetRequiredService<IDatabaseService>();
            EPCISQueryDocument cached = await dbService.QueryEvents(new EPCISQueryParameters
            {
                query = new EPCISQuery { MATCH_anyEPC = new List<string> { LotEpc } }
            });

            // Act: this is what the EPCIS controller writes to the response.
            string served = OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(cached);

            // Assert: the KDEs are on the wire.
            Assert.That(served, Does.Contain("gdst:transportType"), "The served document carries no transport type.");
            Assert.That(served, Does.Contain("SHIP-0001"));
            Assert.That(served, Does.Contain("gdst:productOwner"));
            Assert.That(served, Does.Contain("gdst:humanWelfarePolicy"));

            // Assert: and a reader gets them back, which is what MSC does with the response.
            EPCISQueryDocument reread = OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(served);
            var shipping = reread.Events.Single() as MSCShippingEvent;

            Assert.That(shipping, Is.Not.Null, "The served document does not read back as an MSC shipping event.");
            Assert.That(shipping!.TransportType, Is.EqualTo("SEA"));
            Assert.That(shipping.TransportVehicleID, Is.EqualTo(VehicleId));
            Assert.That(shipping.TransportNumber, Is.EqualTo("SHIP-0001"));
            Assert.That(shipping.TransportProviderID, Is.EqualTo(OwnerPgln));
            Assert.That(shipping.ProductOwner!.ToString(), Is.EqualTo(OwnerPgln));
            Assert.That(shipping.HumanWelfarePolicy, Is.EqualTo("3p"));
        }


        private const string MappingJson = @"
        {
          ""Connections"": {
            ""SOURCE_DB"": {
              ""ConnectorType"": ""SqlServer"",
              ""ConnectionString"": ""server=localhost;database=TraceabilityDriverMscTestDB;Integrated Security=SSPI;TrustServerCertificate=True;"",
              ""Database"": ""TraceabilityDriverMscTestDB""
            }
          },
          ""Dictionaries"": {},
          ""Mappings"": [
            {
              ""Selectors"": [
                {
                  ""Database"": ""SOURCE_DB"",
                  ""Count"": ""SELECT COUNT(*) FROM Shipments WHERE Id > @LastID"",
                  ""Selector"": ""SELECT Id AS idEvent, EventTimeUtc AS eventTime, InfoPgln AS infoPgln, OwnerPgln AS ownerPgln, LocationGln AS locGln, HumanWelfarePolicy AS humanWelfarePolicy, TransportType AS transportType, TransportVehicleId AS transportVehicleId, TransportNumber AS transportNumber, TransportProviderPgln AS transportProviderPgln, Epc AS epc, Quantity AS qty, Uom AS uom, ProductGtin AS prodGtin FROM Shipments WHERE Id > @LastID ORDER BY Id ASC OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;"",
                  ""Memory"": {
                    ""LastID"": { ""DefaultValue"": ""0"", ""Field"": ""$idEvent"", ""DataType"": ""Int64"" }
                  },
                  ""EventMapping"": {
                    ""EventKey"": ""$idEvent"",
                    ""EventType"": ""!mscshippingevent"",
                    ""EventTime"": ""$eventTime"",
                    ""InformationProvider"": { ""OwnerId"": ""$infoPgln"" },
                    ""ProductOwner"": { ""OwnerId"": ""$ownerPgln"" },
                    ""Location"": { ""LocationId"": ""$locGln"" },
                    ""HumanWelfarePolicy"": ""$humanWelfarePolicy"",
                    ""TransportType"": ""$transportType"",
                    ""TransportVehicleID"": ""$transportVehicleId"",
                    ""TransportNumber"": ""$transportNumber"",
                    ""TransportProviderID"": ""$transportProviderPgln"",
                    ""Products"": [
                      {
                        ""ProductId"": ""$epc"",
                        ""ProductType"": ""!Reference"",
                        ""Quantity"": ""$qty"",
                        ""UoM"": ""$uom"",
                        ""ProductDefinition"": { ""ProductDefinitionId"": ""$prodGtin"" }
                      }
                    ]
                  }
                }
              ]
            }
          ]
        }";
    }
}
