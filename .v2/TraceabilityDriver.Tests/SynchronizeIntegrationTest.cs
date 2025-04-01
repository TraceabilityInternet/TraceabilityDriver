using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.Mapping;
using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver.Tests
{
    /// <summary>
    /// This will perform a full test using the synchronize service and an official mapping file.
    /// </summary>
    public class SynchronizeIntegrationTest : IDisposable
    {
        private IServiceProvider _services;
        private Mock<IMappingSource> _mockMappingSource;
        private TestLoggerProvider _loggerProvider;

        [SetUp]
        public void Setup()
        {
            _mockMappingSource = new Mock<IMappingSource>();
            _loggerProvider = new TestLoggerProvider();

            // add default newtonsoft json converter for TDMappingConfiguration
            Newtonsoft.Json.JsonConvert.DefaultSettings = () => new Newtonsoft.Json.JsonSerializerSettings
            {
                Converters = new List<Newtonsoft.Json.JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
            };

            // mock reading the mapping file from an environment variable. if the environment variable
            // does not exist, it should skip the tests.
            _mockMappingSource.Setup(x => x.GetMappings()).Returns(() =>
            {
                string? filePath = Environment.GetEnvironmentVariable("TD_INTEGRATION_TEST_MAPPING_FILE");
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    Assert.Ignore("The environment variable TD_INTEGRATION_TEST_MAPPING_FILE is not set.");
                }

                var json = System.IO.File.ReadAllText(filePath);
                var mapping = Newtonsoft.Json.JsonConvert.DeserializeObject<TDMappingConfiguration>(json)
                    ?? throw new InvalidOperationException($"The mapping file {filePath} could not be deserialized.");
                return new List<TDMappingConfiguration> { mapping };
            });

            // Create IConfiguration from appsettings.Tests.json
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            // Setup the services.
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddProvider(_loggerProvider);
            });
            services.AddSingleton<IConfiguration>(configuration);

            // SERVICES
            services.AddSingleton<IMongoDBService, MongoDBService>();
            services.AddSingleton<ISynchronizeService, SynchronizeService>();
            services.AddHostedService<HostedSyncService>();

            // CONNECTORS
            services.AddSingleton<ITDConnectorFactory, TDConnectorFactory>();
            services.AddTransient<TDSqlServerConnector>();

            // MAPPING
            services.AddScoped<IMappingContext, MappingContext>();
            services.AddSingleton<IMappingSource>(_mockMappingSource.Object);
            services.AddTransient<IEventsTableMappingService, EventsTableMappingService>();
            services.AddTransient<IEventsConverterService, EventsConverterService>();
            services.AddTransient<IEventsMergerService, EventsMergeByIdService>();

            // MAPPING FUNCTIONS
            services.AddSingleton<IMappingFunctionFactory, MappingFunctionFactory>();
            services.AddKeyedTransient<IMappingFunction, DictionaryMappingFunction>("dictionary");
            services.AddKeyedTransient<IMappingFunction, GenerateIdentifierFunction>("generateidentifier");
            services.AddKeyedTransient<IMappingFunction, JoinFunction>("join");

            _services = services.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            Dispose();
        }

        [Test]
        public async Task IntegrateTest_SynchronizeService()
        {
            // Arrange
            var synchronizeService = _services.GetRequiredService<ISynchronizeService>();

            // Act
            await synchronizeService.SynchronizeAsync(TestContext.CurrentContext.CancellationToken);

            // Assert that no errors were logged.
            var errorLogs = _loggerProvider.LogEntries.Where(e => e.Level >= LogLevel.Error).ToList();
            Assert.That(errorLogs, Is.Empty, $"Found {errorLogs.Count} error logs: {string.Join(", ", errorLogs.Select(l => l.Message))}");
        }

        public void Dispose()
        {
            _loggerProvider?.Dispose();

            if (_services is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
