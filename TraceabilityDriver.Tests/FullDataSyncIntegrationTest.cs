using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Queries;
using System.Reflection;
using TraceabilityDriver.Controllers;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Authentication;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.GDST;
using TraceabilityDriver.Services.Mapping;
using TraceabilityDriver.Services.Mapping.Functions;
using TraceabilityDriver.Tests.TestDatabase;

namespace TraceabilityDriver.Tests
{
    /// <summary>
    /// End-to-end integration test proving the driver can sync a full GDST supply chain from an
    /// internal database and produce a traceability record equivalent to the golden capability test
    /// document (FullData.json).
    /// </summary>
    /// <remarks>
    /// The test seeds a dedicated SQL Server source database whose rows mirror FullData.json, syncs it
    /// through the fulldata mapping into the SQL Server data cache, hosts the driver's query endpoints
    /// (EPCIS, master data, digital link) on an in-process TestServer, runs a real OpenTraceability SDK
    /// traceback from the final shipment SSCC, and deep-compares the result to FullData.json ignoring
    /// only event ids and times.
    /// </remarks>
    [TestFixture]
    [Category("AdvancedTest")]
    public class FullDataSyncIntegrationTest : IDisposable
    {
        private const string SourceDatabaseName = "TraceabilityDriverFullDataTestDB";
        private const string SourceConnectionString = "Server=127.0.0.1,3433;Database=" + SourceDatabaseName + ";User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";
        private const string MappingResourceName = "TraceabilityDriver.Tests.TestDatabase.fulldata_mapping_sqlserver.json";
        private const string StartSscc = "urn:epc:id:sscc:08600031303.solution1";

        private IServiceProvider? _services;
        private TestLoggerProvider? _loggerProvider;
        private IHost? _queryHost;

        /// <summary>
        /// Initializes the OpenTraceability GDST mappers once for the fixture.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            OpenTraceability.GDST.Setup.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            Dispose();
        }

        /// <summary>
        /// Syncing the seeded source database and tracing back from the final shipment SSCC must yield
        /// a document equivalent to FullData.json (ignoring event ids and times).
        /// </summary>
        [Test]
        public async Task Sync_FullData_SqlServerSource_SqlServerCache_TracebackMatchesGoldenDocument()
        {
            if (ShouldSkip())
            {
                Assert.Ignore("Skipping integration tests because NO_SQL_DB environment variable is set to true.");
                return;
            }

            // Arrange: seed the dedicated source database with the FullData rows.
            SetupSourceDatabase();

            // Arrange: load the mapping and build the sync services.
            TDMappingConfiguration mapping = LoadMapping();
            var mockMappingSource = new Mock<IMappingSource>();
            mockMappingSource.Setup(x => x.GetMappings()).Returns(new List<TDMappingConfiguration> { mapping });

            IConfiguration configuration = BuildConfiguration();
            _services = BuildSyncServices(configuration, mockMappingSource);

            // Clear the target data cache before syncing.
            IDatabaseService dbService = _services.GetRequiredService<IDatabaseService>();
            await dbService.ClearDatabaseAsync();

            // Act: sync the source database into the data cache.
            ISynchronizeService synchronizeService = _services.GetRequiredService<ISynchronizeService>();
            await synchronizeService.SynchronizeAsync(TestContext.CurrentContext.CancellationToken);

            // Assert: the sync completed without any errors (conversion problems surface here).
            List<(LogLevel Level, string Message, Exception? Exception)> errorLogs = _loggerProvider!.LogEntries.Where(e => e.Level >= LogLevel.Error).ToList();
            Assert.That(errorLogs, Is.Empty, $"Found {errorLogs.Count} error logs during sync:\n" + string.Join("\n", errorLogs.Select(l => $"  - {l.Message}")));

            // Act: host the query endpoints in-process and run a real SDK traceback from the final SSCC.
            _queryHost = await StartQueryHostAsync(configuration);
            EPCISDocument actual = await TracebackAsync(_queryHost);

            // Assert: the traceback produced the complete chain.
            Assert.That(actual.Events, Has.Count.EqualTo(24), "The traceback should recover all 24 events of the golden document.");

            // Assert: the traceback result is equivalent to the golden document.
            EPCISDocument expected = LoadGoldenDocument();
            List<string> diffs = FullDataComparer.CompareDocuments(expected, actual);
            Assert.That(diffs, Is.Empty, $"The traceback result differs from FullData.json in {diffs.Count} place(s):\n" + string.Join("\n", diffs));
        }

        private static bool ShouldSkip()
        {
            return Environment.GetEnvironmentVariable("NO_SQL_DB")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        /// <summary>
        /// Drops and recreates the dedicated source database, then builds and seeds the fulldata schema.
        /// </summary>
        private static void SetupSourceDatabase()
        {
            var config = new TestDatabaseConfig
            {
                ConnectionString = SourceConnectionString,
                DatabaseName = SourceDatabaseName,
                BuildCommand = ReadEmbeddedResource("TraceabilityDriver.Tests.TestDatabase.fulldata_build.sql"),
                SeedCommand = ReadEmbeddedResource("TraceabilityDriver.Tests.TestDatabase.fulldata_seed.sql")
            };
            new TestMSSQLDatabase(config).SetupDatabase();
        }

        /// <summary>
        /// Loads the fulldata mapping from the embedded resource and points it at the source database.
        /// </summary>
        private static TDMappingConfiguration LoadMapping()
        {
            string json = ReadEmbeddedResource(MappingResourceName);
            TDMappingConfiguration mapping = Newtonsoft.Json.JsonConvert.DeserializeObject<TDMappingConfiguration>(json)
                ?? throw new InvalidOperationException($"Failed to deserialize mapping from {MappingResourceName}.");

            mapping.Connections["SOURCE_DB"].ConnectionString = SourceConnectionString;
            return mapping;
        }

        /// <summary>
        /// Builds the test configuration: the shared test settings plus the public URL the digital link
        /// resolver embeds in its linksets, pointed at the in-process TestServer.
        /// </summary>
        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.Tests.json")
                .AddInMemoryCollection(new Dictionary<string, string?> { ["URL"] = "http://localhost" })
                .Build();
        }

        /// <summary>
        /// Builds the DI container for the sync, mirroring the Startup registrations for a SQL Server
        /// data cache with a mocked mapping source.
        /// </summary>
        private IServiceProvider BuildSyncServices(IConfiguration configuration, Mock<IMappingSource> mockMappingSource)
        {
            _loggerProvider = new TestLoggerProvider();

            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddProvider(_loggerProvider);
            });
            services.AddSingleton<IConfiguration>(configuration);

            Newtonsoft.Json.JsonConvert.DefaultSettings = () => new Newtonsoft.Json.JsonSerializerSettings
            {
                Converters = new List<Newtonsoft.Json.JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
            };

            // Data cache (target).
            string sqlConnStr = configuration["SqlServer:ConnectionString"]
                ?? throw new InvalidOperationException("SqlServer:ConnectionString not found in appsettings.Tests.json");
            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(sqlConnStr, sqlOpts => sqlOpts.EnableRetryOnFailure());
            });
            services.AddScoped<IDatabaseService, SqlServerService>();

            services.AddSingleton<ISynchronizeService, SynchronizeService>();

            // Connectors.
            services.AddSingleton<ITDConnectorFactory, TDConnectorFactory>();
            services.AddTransient<TDSqlServerConnector>();

            // Mapping.
            services.AddScoped<ISynchronizationContext, TraceabilityDriver.Services.SynchronizationContext>();
            services.AddSingleton<IMappingSource>(mockMappingSource.Object);
            services.AddTransient<IEventsTableMappingService, EventsTableMappingService>();
            services.AddTransient<IEventsConverterService, EventsConverterService>();
            services.AddTransient<IEventsMergerService, EventsMergeByIdService>();

            // Mapping functions.
            services.AddSingleton<IMappingFunctionFactory, MappingFunctionFactory>();
            services.AddKeyedTransient<IMappingFunction, DictionaryMappingFunction>("dictionary");
            services.AddKeyedTransient<IMappingFunction, GenerateIdentifierFunction>("generateidentifier");
            services.AddKeyedTransient<IMappingFunction, JoinFunction>("join");

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Hosts the driver's query endpoints (EPCIS, master data, digital link) on an in-process
        /// TestServer reading from the same SQL Server data cache the sync wrote to.
        /// </summary>
        private async Task<IHost> StartQueryHostAsync(IConfiguration configuration)
        {
            string sqlConnStr = configuration["SqlServer:ConnectionString"]!;

            IHost host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddSingleton<IConfiguration>(configuration);
                        services.AddLogging(builder => builder.AddProvider(_loggerProvider!));
                        services.AddDbContextFactory<ApplicationDbContext>(options =>
                        {
                            options.UseSqlServer(sqlConnStr, sqlOpts => sqlOpts.EnableRetryOnFailure());
                        });
                        services.AddScoped<IDatabaseService, SqlServerService>();
                        services.AddScoped<IDigitalLinkService, DigitalLinkService>();

                        // The controllers are decorated with [Authorize]; the always-authenticated
                        // scheme satisfies them the same way Startup does when no auth is configured.
                        services.AddAuthentication("AlwaysAuthenticated")
                            .AddScheme<AuthenticationSchemeOptions, AlwaysAuthenticatedHandler>("AlwaysAuthenticated", null);
                        services.AddAuthorization(options =>
                        {
                            options.AddPolicy("TracebackApiKey", policy => policy.AddAuthenticationSchemes("AlwaysAuthenticated").RequireAuthenticatedUser());
                        });

                        services.AddControllers().AddApplicationPart(typeof(EPCISController).Assembly);
                    });
                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                })
                .StartAsync();

            return host;
        }

        /// <summary>
        /// Runs the OpenTraceability SDK traceback from the final shipment SSCC against the TestServer,
        /// resolves the GDST master data for the recovered events, and returns the merged document.
        /// </summary>
        private static async Task<EPCISDocument> TracebackAsync(IHost queryHost)
        {
            HttpClient client = queryHost.GetTestServer().CreateClient();

            DigitalLinkQueryOptions resolverOptions = new DigitalLinkQueryOptions
            {
                URL = new Uri("http://localhost/digitallink/")
            };

            EPC startEpc = new EPC(StartSscc);

            // Discover the EPCIS query interface through the digital link resolver, then trace back.
            Uri? epcisUrl = await EPCISTraceabilityResolver.GetEPCISQueryInterfaceURL(resolverOptions, startEpc, client);
            Assert.That(epcisUrl, Is.Not.Null, "The digital link resolver did not return an EPCIS query interface URL for the start SSCC.");

            EPCISQueryInterfaceOptions queryOptions = new EPCISQueryInterfaceOptions
            {
                URL = epcisUrl,
                Version = EPCISVersion.V2,
                Format = EPCISDataFormat.JSON
            };

            EPCISQueryResults results = await EPCISTraceabilityResolver.Traceback(queryOptions, startEpc, client);
            Assert.That(results.Errors, Is.Empty, $"The traceback reported errors:\n" + string.Join("\n", results.Errors.Select(e => $"  - {e.Details ?? e.Type.ToString()}")));
            Assert.That(results.Document, Is.Not.Null, "The traceback returned no document.");

            // Merge into a full EPCIS document and dedupe events, mirroring the driver's own
            // TracebackService flow, then resolve the GDST master data for everything recovered.
            EPCISDocument document = new EPCISDocument();
            document.EPCISVersion = EPCISVersion.V2;
            document.CreationDate = DateTimeOffset.UtcNow;
            document.Merge(results.Document!);
            document.Events = document.Events.GroupBy(e => e.EventID.ToString()).Select(g => g.First()).ToList();

            await OpenTraceability.GDST.GDSTMasterDataResolver.ResolveGDSTMasterData(resolverOptions, document, client);

            return document;
        }

        /// <summary>
        /// Loads the golden FullData.json document from the driver assembly, exactly as the capability
        /// test service does.
        /// </summary>
        private static EPCISDocument LoadGoldenDocument()
        {
            using Stream stream = typeof(GDSTCapabilityTestService).Assembly.GetManifestResourceStream("TraceabilityDriver.Services.GDST.FullData.json")
                ?? throw new InvalidOperationException("The resource 'FullData.json' was not found in the driver assembly.");
            using StreamReader reader = new StreamReader(stream);

            return OpenTraceability.Mappers.OpenTraceabilityMappers.EPCISDocument.JSON.Map(reader.ReadToEnd());
        }

        private static string ReadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public void Dispose()
        {
            _queryHost?.Dispose();
            _queryHost = null;

            _loggerProvider?.Dispose();
            _loggerProvider = null;

            if (_services is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _services = null;
        }
    }
}
