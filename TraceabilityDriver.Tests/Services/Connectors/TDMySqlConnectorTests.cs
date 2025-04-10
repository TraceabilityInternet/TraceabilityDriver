using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Data;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services;
using MySql.Data.MySqlClient;

namespace TraceabilityDriver.Tests.Services.Connectors
{
    [TestFixture]
    public class TDMySqlConnectorTests
    {
        private string _connectionString = "server=localhost;database=test;user=root;password=your_password;";
        private bool _skipTests;

        private readonly Mock<IOptions<TDConnectorConfiguration>> _mockOptions;
        private readonly TDConnectorConfiguration _configuration;
        private readonly Mock<ILogger<TDMySqlConnector>> _mockLogger;
        private readonly Mock<IEventsTableMappingService> _mockEventsTableMappingService;
        private readonly Mock<ISynchronizationContext> _mockSyncContext;
        private readonly TDMySqlConnector _connector;

        public TDMySqlConnectorTests()
        {
            _skipTests = Environment.GetEnvironmentVariable("NO_SQL_DB")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

            _configuration = new TDConnectorConfiguration();
            _mockOptions = new Mock<IOptions<TDConnectorConfiguration>>();
            _mockOptions.Setup(o => o.Value).Returns(_configuration);

            _mockLogger = new Mock<ILogger<TDMySqlConnector>>();
            _mockEventsTableMappingService = new Mock<IEventsTableMappingService>();
            _mockSyncContext = new Mock<ISynchronizationContext>();

            // Set up the synchronization context with required properties
            _mockSyncContext.Setup(s => s.CurrentSync).Returns(new SyncHistoryItem());

            _connector = new TDMySqlConnector(
                _mockLogger.Object,
                _mockEventsTableMappingService.Object,
                _mockSyncContext.Object);
        }

        [OneTimeSetUp]
        public void Setup()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Setup code for MySQL database if needed
        }

        [Test]
        public async Task TestConnectionAsync_ShouldReturnTrue_WhenConnectionIsSuccessful()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Arrange
            _configuration.ConnectionString = _connectionString;

            // Act
            var result = await _connector.TestConnectionAsync(_configuration);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void TestConnectionAsync_ShouldThrowException_WhenConnectionFails()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Arrange
            _configuration.ConnectionString = "InvalidConnectionString";

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => _connector.TestConnectionAsync(_configuration));
        }

        [Test]
        public async Task GetEventsAsync_ShouldReturnEvents_WhenQueryIsSuccessful()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Arrange
            var selector = new TDMappingSelector
            {
                Selector = "SELECT * FROM Events",
                Count = "SELECT COUNT(*) FROM Events",
                EventMapping = new TDEventMapping(),
                Memory = new Dictionary<string, TDMappingSelectorMemoryVariable>()
            };

            var dataTable = new DataTable();
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Rows.Add(1, "Event1");

            _configuration.ConnectionString = _connectionString;
            _configuration.Database = "test";

            // Setup the sync context with necessary properties for GetEventsAsync
            var syncHistoryItem = new SyncHistoryItem { Memory = new Dictionary<string, string>() };
            _mockSyncContext.Setup(s => s.CurrentSync).Returns(syncHistoryItem);

            _mockEventsTableMappingService.Setup(m => m.MapEvents(selector.EventMapping, It.IsAny<DataTable>(), It.IsAny<CancellationToken>()))
                .Returns(new List<CommonEvent> { new CommonEvent { EventId = "123", EventType = "Fishing" } });

            // Act
            var events = await _connector.GetEventsAsync(_configuration, selector, CancellationToken.None);

            // Assert
            Assert.That(events, Is.Not.Null);
            Assert.That(events.Count(), Is.EqualTo(1));
            Assert.That(events.First().EventId, Is.EqualTo("123"));
            Assert.That(events.First().EventType, Is.EqualTo("Fishing"));

            // Verify that the sync context was updated
            _mockSyncContext.Verify(s => s.Updated(), Times.AtLeastOnce);
        }

        [Test]
        public void GetEventsAsync_ShouldThrowException_WhenQueryFails()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Arrange
            var selector = new TDMappingSelector
            {
                Selector = "SELECT * FROM InvalidTable",
                Count = "SELECT COUNT(*) FROM InvalidTable",
                EventMapping = new TDEventMapping(),
                Memory = new Dictionary<string, TDMappingSelectorMemoryVariable>()
            };

            _configuration.ConnectionString = _connectionString;

            // Setup sync context
            var syncHistoryItem = new SyncHistoryItem { Memory = new Dictionary<string, string>() };
            _mockSyncContext.Setup(s => s.CurrentSync).Returns(syncHistoryItem);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => _connector.GetEventsAsync(_configuration, selector, CancellationToken.None));
        }

        [Test]
        public void HandleMemoryVariables_ShouldUpdateMemory_WhenColumnExists()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Arrange
            var dataTable = new DataTable();
            dataTable.Columns.Add("TestColumn", typeof(string));
            dataTable.Rows.Add("Value1");
            dataTable.Rows.Add("Value2");
            dataTable.Rows.Add("FinalValue");

            var memory = new TDMappingSelectorMemoryVariable
            {
                Field = "$TestColumn",
                DataType = "string"
            };

            var syncHistoryItem = new SyncHistoryItem { Memory = new Dictionary<string, string>() };
            _mockSyncContext.Setup(s => s.CurrentSync).Returns(syncHistoryItem);

            // Act
            _connector.HandleMemoryVariables(dataTable, "testKey", memory);

            // Assert
            Assert.That(_mockSyncContext.Object.CurrentSync.Memory["testKey"], Is.EqualTo("FinalValue"));
        }

        [Test]
        public void HandleMemoryVariables_ShouldThrowException_WhenColumnDoesNotExist()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Arrange
            var dataTable = new DataTable();
            dataTable.Columns.Add("ExistingColumn", typeof(string));

            var memory = new TDMappingSelectorMemoryVariable
            {
                Field = "$NonExistentColumn",
                DataType = "string"
            };

            // Act & Assert
            Assert.Throws<Exception>(() => _connector.HandleMemoryVariables(dataTable, "testKey", memory));
        }

        [Test]
        public void AddMemoryVariable_ShouldAddParameter_ForDifferentDataTypes()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Arrange
            var command = new MySqlCommand();

            var memoryVariables = new Dictionary<string, TDMappingSelectorMemoryVariable>
            {
                { "stringVar", new TDMappingSelectorMemoryVariable { DataType = "string", DefaultValue = "test" } },
                { "int32Var", new TDMappingSelectorMemoryVariable { DataType = "int32", DefaultValue = "123" } },
                { "int64Var", new TDMappingSelectorMemoryVariable { DataType = "int64", DefaultValue = "9223372036854775807" } },
                { "doubleVar", new TDMappingSelectorMemoryVariable { DataType = "double", DefaultValue = "123.45" } },
                { "datetimeVar", new TDMappingSelectorMemoryVariable { DataType = "datetime", DefaultValue = "2023-01-01" } },
                { "boolVar", new TDMappingSelectorMemoryVariable { DataType = "bool", DefaultValue = "true" } }
            };

            // Act
            foreach (var variable in memoryVariables)
            {
                _connector.AddMemoryVariable(command, variable.Key, variable.Value);
            }

            // Assert
            Assert.That(command.Parameters.Count, Is.EqualTo(6));
            Assert.That(command.Parameters["@stringVar"].MySqlDbType, Is.EqualTo(MySqlDbType.VarChar));
            Assert.That(command.Parameters["@stringVar"].Value, Is.EqualTo("test"));

            Assert.That(command.Parameters["@int32Var"].MySqlDbType, Is.EqualTo(MySqlDbType.Int32));
            Assert.That(command.Parameters["@int32Var"].Value, Is.EqualTo(123));

            Assert.That(command.Parameters["@int64Var"].MySqlDbType, Is.EqualTo(MySqlDbType.Int64));
            Assert.That(command.Parameters["@int64Var"].Value, Is.EqualTo(9223372036854775807L));

            Assert.That(command.Parameters["@doubleVar"].MySqlDbType, Is.EqualTo(MySqlDbType.Double));
            Assert.That(command.Parameters["@doubleVar"].Value, Is.EqualTo(123.45));

            Assert.That(command.Parameters["@datetimeVar"].MySqlDbType, Is.EqualTo(MySqlDbType.DateTime));
            Assert.That(command.Parameters["@datetimeVar"].Value, Is.EqualTo(new DateTime(2023, 1, 1)));

            Assert.That(command.Parameters["@boolVar"].MySqlDbType, Is.EqualTo(MySqlDbType.Bit));
            Assert.That(command.Parameters["@boolVar"].Value, Is.EqualTo(true));
        }

        [Test]
        public void AddMemoryVariable_ShouldUseValueFromPreviousSync_WhenAvailable()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Arrange
            var command = new MySqlCommand();

            var memory = new TDMappingSelectorMemoryVariable
            {
                DataType = "string",
                DefaultValue = "defaultValue"
            };

            var previousSyncHistory = new SyncHistoryItem
            {
                Memory = new Dictionary<string, string>
                {
                    { "existingKey", "previousValue" }
                }
            };

            _mockSyncContext.Setup(s => s.PreviousSync).Returns(previousSyncHistory);

            // Act
            _connector.AddMemoryVariable(command, "existingKey", memory);

            // Assert
            Assert.That(command.Parameters["@existingKey"].Value, Is.EqualTo("previousValue"));
        }

        [Test]
        public void AddMemoryVariable_ShouldThrowException_ForInvalidDataType()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Arrange
            var command = new MySqlCommand();

            var memory = new TDMappingSelectorMemoryVariable
            {
                DataType = "invalidType",
                DefaultValue = "value"
            };

            // Act & Assert
            Assert.Throws<Exception>(() => _connector.AddMemoryVariable(command, "testKey", memory));
        }
    }
}
