using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTraceability.Models.Events;
using System.Net;
using TraceabilityDriver.Extensions;
using System.Reflection;
using System.Text;
using TraceabilityDriver.Controllers;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.Mapping;
using TraceabilityDriver.Services.Mapping.Functions;
using TraceabilityDriver.Tests.TestDatabase;
using OpenTraceability.GDST.Events;

namespace TraceabilityDriver.Tests
{
    public enum SourceConnectorType
    {
        SqlServer,
        PostgreSQL,
        MySQL
    }

    public enum DatabaseServiceType
    {
        MongoDB,
        SqlServer
    }

    [TestFixture]
    public class SynchronizeServiceIntegrationTest : IDisposable
    {
        private static readonly Dictionary<SourceConnectorType, string> SourceConnectionStrings = new()
        {
            [SourceConnectorType.SqlServer] = "Server=127.0.0.1,3433;Database=TraceabilityDriverTestDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;",
            [SourceConnectorType.PostgreSQL] = "Host=127.0.0.1;Port=5433;Database=TraceabilityDriverTestDB;Username=postgres;Password=YourStrong!Passw0rd;",
            [SourceConnectorType.MySQL] = "server=127.0.0.1;port=3307;database=TraceabilityDriverTestDB;user=root;password=YourStrong!Passw0rd;AllowUserVariables=true;",
        };

        private static readonly Dictionary<SourceConnectorType, string> MappingResourceNames = new()
        {
            [SourceConnectorType.SqlServer] = "TraceabilityDriver.Tests.TestDatabase.test_mapping_sqlserver.json",
            [SourceConnectorType.PostgreSQL] = "TraceabilityDriver.Tests.TestDatabase.test_mapping_postgresql.json",
            [SourceConnectorType.MySQL] = "TraceabilityDriver.Tests.TestDatabase.test_mapping_mysql.json",
        };

        private IServiceProvider? _services;
        private TestLoggerProvider? _loggerProvider;

        [TearDown]
        public void TearDown()
        {
            Dispose();
        }

        #region Individual Tests

        [Test]
        public async Task Sync_SqlServer_To_MongoDB()
        {
            await RunSyncTest(SourceConnectorType.SqlServer, DatabaseServiceType.MongoDB);
        }

        [Test]
        public async Task Sync_SqlServer_To_SqlServer()
        {
            await RunSyncTest(SourceConnectorType.SqlServer, DatabaseServiceType.SqlServer);
        }

        [Test]
        public async Task Sync_PostgreSQL_To_MongoDB()
        {
            await RunSyncTest(SourceConnectorType.PostgreSQL, DatabaseServiceType.MongoDB);
        }

        [Test]
        public async Task Sync_PostgreSQL_To_SqlServer()
        {
            await RunSyncTest(SourceConnectorType.PostgreSQL, DatabaseServiceType.SqlServer);
        }

        [Test]
        public async Task Sync_MySQL_To_MongoDB()
        {
            await RunSyncTest(SourceConnectorType.MySQL, DatabaseServiceType.MongoDB);
        }

        [Test]
        public async Task Sync_MySQL_To_SqlServer()
        {
            await RunSyncTest(SourceConnectorType.MySQL, DatabaseServiceType.SqlServer);
        }

        #endregion

        #region Helpers

        private async Task RunSyncTest(SourceConnectorType sourceConnector, DatabaseServiceType dbServiceType)
        {
            if (ShouldSkip())
            {
                Assert.Ignore("Skipping integration tests because NO_SQL_DB environment variable is set to true.");
                return;
            }

            // Arrange: seed source database
            SetupSourceDatabase(sourceConnector);

            // Arrange: load mapping and build services
            var mapping = LoadMapping(sourceConnector);
            var mockMappingSource = new Mock<IMappingSource>();
            mockMappingSource.Setup(x => x.GetMappings()).Returns(new List<TDMappingConfiguration> { mapping });

            _services = BuildServices(sourceConnector, dbServiceType, mockMappingSource);

            // Clear the target database before syncing
            var dbService = _services.GetRequiredService<IDatabaseService>();
            await dbService.ClearDatabaseAsync();

            var synchronizeService = _services.GetRequiredService<ISynchronizeService>();

            // Act
            await synchronizeService.SynchronizeAsync(TestContext.CurrentContext.CancellationToken);

            // Assert: no errors were logged
            var errorLogs = _loggerProvider!.LogEntries.Where(e => e.Level >= LogLevel.Error).ToList();
            Assert.That(errorLogs, Is.Empty,
                $"[{sourceConnector} -> {dbServiceType}] Found {errorLogs.Count} error logs:\n" +
                string.Join("\n", errorLogs.Select(l => $"  - {l.Message}")));

            // check controllers return complete event data
            var epcisLogger = _services.GetRequiredService<ILogger<EPCISController>>();
            var epcisController = new EPCISController(dbService, epcisLogger);
            var epcisRequestContext = new DefaultHttpContext();
            epcisRequestContext.Request.Headers["Accept"] = "application/json";
            epcisRequestContext.Request.Headers["GS1-EPCIS-Version"] = "2.0";
            epcisRequestContext.Request.Scheme = "https";
            epcisRequestContext.Request.Host = new HostString("localhost");
            string timeStamp = DateTime.UtcNow.AddMinutes(-10).ToString("o"); // ISO 8601 format
            epcisRequestContext.Request.Path = $"/epcis/events";
            epcisRequestContext.Request.QueryString = new QueryString($"?LE_eventTime={Uri.EscapeDataString(timeStamp)}");
            MemoryStream responseStream = new MemoryStream();
            epcisRequestContext.Response.Body = responseStream;

            var epcisControllerContext = new ControllerContext
            {
                HttpContext = epcisRequestContext,
            };
            epcisController.ControllerContext = epcisControllerContext;
            var result = await epcisController.SimpleQueryGet();

            // Assert result type if you want
            Assert.That(result, Is.TypeOf<EmptyResult>());

            // Assert status code
            Assert.That(epcisRequestContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));

            responseStream.Position = 0;
            string responseText;
            using (var reader = new StreamReader(responseStream, Encoding.UTF8, leaveOpen: true))
            {
                responseText = await reader.ReadToEndAsync();
            }
            Assert.That(responseText, Is.Not.Null);
            Assert.That(responseText, Is.Not.Empty);

            EPCISQueryDocument doc = new();
            doc = doc.FromJson(responseText);
            Assert.That(doc.Events, Is.Not.Null);
            Assert.That(doc.Events, Is.Not.Empty);
            
            // The GDST 2.0 model collapses the semantic 1.2 events into the generic event set, so
            // the mappings produce one of each generic type; the business meaning is carried by the
            // product/location classifications on the master data.
            GDSTAggregationEvent? aggEvent = doc.Events.OfType<GDSTAggregationEvent>().FirstOrDefault();
            Assert.That(aggEvent, Is.Not.Null, "Expected at least one GDSTAggregationEvent in the response.");

            GDSTDisaggregationEvent? disaggEvent = doc.Events.OfType<GDSTDisaggregationEvent>().FirstOrDefault();
            Assert.That(disaggEvent, Is.Not.Null, "Expected at least one GDSTDisaggregationEvent in the response.");

            GDSTCommissionEvent? commissionEvent = doc.Events.OfType<GDSTCommissionEvent>().FirstOrDefault();
            Assert.That(commissionEvent, Is.Not.Null, "Expected at least one GDSTCommissionEvent in the response.");

            GDSTShippingEvent? shippingEvent = doc.Events.OfType<GDSTShippingEvent>().FirstOrDefault();
            Assert.That(shippingEvent, Is.Not.Null, "Expected at least one GDSTShippingEvent in the response.");

            GDSTReceivingEvent? receivingEvent = doc.Events.OfType<GDSTReceivingEvent>().FirstOrDefault();
            Assert.That(receivingEvent, Is.Not.Null, "Expected at least one GDSTReceivingEvent in the response.");

            GDSTTransformationEvent? transformationEvent = doc.Events.OfType<GDSTTransformationEvent>().FirstOrDefault();
            Assert.That(transformationEvent, Is.Not.Null, "Expected at least one GDSTTransformationEvent in the response.");
        }

        private static bool ShouldSkip()
        {
            return Environment.GetEnvironmentVariable("NO_SQL_DB")
                ?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        private void SetupSourceDatabase(SourceConnectorType sourceConnector)
        {
            string connectionString = SourceConnectionStrings[sourceConnector];

            switch (sourceConnector)
            {
                case SourceConnectorType.SqlServer:
                {
                    string buildSql = ReadEmbeddedResource("TraceabilityDriver.Tests.TestDatabase.build.sql");
                    string seedSql = ReadEmbeddedResource("TraceabilityDriver.Tests.TestDatabase.seed.sql");
                    var config = new TestDatabaseConfig
                    {
                        ConnectionString = connectionString,
                        DatabaseName = "TraceabilityDriverTestDB",
                        BuildCommand = buildSql,
                        SeedCommand = seedSql
                    };
                    new TestMSSQLDatabase(config).SetupDatabase();
                    break;
                }
                case SourceConnectorType.PostgreSQL:
                {
                    var config = new TestDatabaseConfig
                    {
                        ConnectionString = connectionString,
                        DatabaseName = "TraceabilityDriverTestDB"
                    };
                    new TestPostgreSQLDatabase(config).SetupDatabase();
                    break;
                }
                case SourceConnectorType.MySQL:
                {
                    var config = new TestDatabaseConfig
                    {
                        ConnectionString = connectionString,
                        DatabaseName = "TraceabilityDriverTestDB"
                    };
                    new TestMySQLDatabase(config).SetupDatabase();
                    break;
                }
            }
        }

        private IServiceProvider BuildServices(
            SourceConnectorType sourceConnector,
            DatabaseServiceType dbServiceType,
            Mock<IMappingSource> mockMappingSource)
        {
            _loggerProvider = new TestLoggerProvider();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Tests.json")
                .Build();

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

            // Database service (target)
            switch (dbServiceType)
            {
                case DatabaseServiceType.MongoDB:
                    services.AddSingleton<IDatabaseService, MongoDBService>();
                    break;
                case DatabaseServiceType.SqlServer:
                    string sqlConnStr = configuration["SqlServer:ConnectionString"]
                        ?? throw new InvalidOperationException("SqlServer:ConnectionString not found in appsettings.Tests.json");
                    services.AddDbContextFactory<ApplicationDbContext>(options =>
                    {
                        options.UseSqlServer(sqlConnStr, sqlOpts => sqlOpts.EnableRetryOnFailure());
                    });
                    services.AddScoped<IDatabaseService, SqlServerService>();
                    break;
            }

            services.AddSingleton<ISynchronizeService, SynchronizeService>();

            // Connectors
            services.AddSingleton<ITDConnectorFactory, TDConnectorFactory>();
            services.AddTransient<TDSqlServerConnector>();
            services.AddTransient<TDPostGreSQLConnector>();
            services.AddTransient<TDMySqlConnector>();

            // Mapping
            services.AddScoped<ISynchronizationContext, TraceabilityDriver.Services.SynchronizationContext>();
            services.AddSingleton<IMappingSource>(mockMappingSource.Object);
            services.AddTransient<IEventsTableMappingService, EventsTableMappingService>();
            services.AddTransient<IEventsConverterService, EventsConverterService>();
            services.AddTransient<IEventsMergerService, EventsMergeByIdService>();

            // Mapping functions
            services.AddSingleton<IMappingFunctionFactory, MappingFunctionFactory>();
            services.AddKeyedTransient<IMappingFunction, DictionaryMappingFunction>("dictionary");
            services.AddKeyedTransient<IMappingFunction, GenerateIdentifierFunction>("generateidentifier");
            services.AddKeyedTransient<IMappingFunction, JoinFunction>("join");

            return services.BuildServiceProvider();
        }

        private TDMappingConfiguration LoadMapping(SourceConnectorType sourceConnector)
        {
            string resourceName = MappingResourceNames[sourceConnector];
            string json = ReadEmbeddedResource(resourceName);
            var mapping = Newtonsoft.Json.JsonConvert.DeserializeObject<TDMappingConfiguration>(json)
                ?? throw new InvalidOperationException($"Failed to deserialize mapping from {resourceName}.");

            string connectionString = SourceConnectionStrings[sourceConnector];
            mapping.Connections["SOURCE_DB"].ConnectionString = connectionString;

            return mapping;
        }

        private static string ReadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        #endregion

        public void Dispose()
        {
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
