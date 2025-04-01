using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Connectors;

namespace TraceabilityDriver.Tests.Services.Connectors
{
    public class TDSqlServerConnectorTests
    {
        private string _connectionString = "server=localhost;database=master;Integrated Security=SSPI;TrustServerCertificate=True;";

        private readonly Mock<IOptions<TDConnectorConfiguration>> _mockOptions;
        private readonly TDConnectorConfiguration _configuration;
        private readonly Mock<ILogger<TDSqlServerConnector>> _mockLogger;
        private readonly Mock<IEventsTableMappingService> _mockEventsTableMappingService;
        private readonly Mock<ISynchronizationContext> _mockSyncContext;
        private readonly TDSqlServerConnector _connector;

        public TDSqlServerConnectorTests()
        {
            _configuration = new TDConnectorConfiguration();
            _mockOptions = new Mock<IOptions<TDConnectorConfiguration>>();
            _mockOptions.Setup(o => o.Value).Returns(_configuration);

            _mockLogger = new Mock<ILogger<TDSqlServerConnector>>();
            _mockEventsTableMappingService = new Mock<IEventsTableMappingService>();
            _mockSyncContext = new Mock<ISynchronizationContext>();

            // Set up the synchronization context with required properties
            _mockSyncContext.Setup(s => s.CurrentSync).Returns(new SyncHistoryItem());

            _connector = new TDSqlServerConnector(
                _mockLogger.Object,
                _mockEventsTableMappingService.Object,
                _mockSyncContext.Object);
        }

        [OneTimeSetUp]
        public void Setup()
        {
            // We need to restore our backup of the database.
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                try
                {
                    // Get the path to the backup file
                    string executingAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location!;
                    string backupPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(executingAssemblyPath)!, "Data", "TestEventsDatabase.bak");

                    // Copy the backup to the SQL Server backup folder
                    string tempBackupPath = @"C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\TestEventsDatabase.bak";
                    System.IO.File.Copy(backupPath, tempBackupPath, true);

                    // First force close all existing connections to the TestEventsDatabase
                    using (var killConnectionsCommand = connection.CreateCommand())
                    {
                        killConnectionsCommand.CommandText = @"
                                IF EXISTS(SELECT * FROM sys.databases WHERE name = 'TestEventsDatabase')
                                BEGIN
                                    ALTER DATABASE TestEventsDatabase SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                END";
                        killConnectionsCommand.ExecuteNonQuery();
                    }

                    // Check if database exists and drop it
                    using (var dropCommand = connection.CreateCommand())
                    {
                        dropCommand.CommandText = @"
                                IF EXISTS(SELECT * FROM sys.databases WHERE name = 'TestEventsDatabase')
                                BEGIN
                                    DROP DATABASE TestEventsDatabase;
                                END";
                        dropCommand.ExecuteNonQuery();
                    }

                    // Restore the database from backup
                    using (var restoreCommand = connection.CreateCommand())
                    {
                        restoreCommand.CommandText = $@"RESTORE DATABASE TestEventsDatabase FROM DISK = '{tempBackupPath}' WITH REPLACE";
                        restoreCommand.ExecuteNonQuery();
                    }

                    // Set database back to multi-user mode
                    using (var multiUserCommand = connection.CreateCommand())
                    {
                        multiUserCommand.CommandText = "ALTER DATABASE TestEventsDatabase SET MULTI_USER";
                        multiUserCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    _mockLogger.Object.LogError(ex, "Error setting up test database");
                    throw;
                }
            }
        }

        [Test]
        public async Task TestConnectionAsync_ShouldReturnTrue_WhenConnectionIsSuccessful()
        {
            // Arrange
            _configuration.ConnectionString = _connectionString.Replace("=master;", "=TestEventsDatabase;");

            // Act
            var result = await _connector.TestConnectionAsync(_configuration);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void TestConnectionAsync_ShouldThrowException_WhenConnectionFails()
        {
            // Arrange
            _configuration.ConnectionString = "InvalidConnectionString";

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => _connector.TestConnectionAsync(_configuration));
        }

        [Test]
        public async Task GetEventsAsync_ShouldReturnEvents_WhenQueryIsSuccessful()
        {
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

            _configuration.ConnectionString = _connectionString.Replace("=master;", "=TestEventsDatabase;");
            _configuration.Database = "TestEventsDatabase";

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
            // Arrange
            var selector = new TDMappingSelector
            {
                Selector = "SELECT * FROM InvalidTable",
                Count = "SELECT COUNT(*) FROM InvalidTable",
                EventMapping = new TDEventMapping(),
                Memory = new Dictionary<string, TDMappingSelectorMemoryVariable>()
            };

            _configuration.ConnectionString = _connectionString.Replace("=master;", "=TestEventsDatabase;");

            // Setup sync context
            var syncHistoryItem = new SyncHistoryItem { Memory = new Dictionary<string, string>() };
            _mockSyncContext.Setup(s => s.CurrentSync).Returns(syncHistoryItem);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => _connector.GetEventsAsync(_configuration, selector, CancellationToken.None));
        }

        [Test]
        public void HandleMemoryVariables_ShouldUpdateMemory_WhenColumnExists()
        {
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
            // Arrange
            var command = new SqlCommand();

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
            Assert.That(command.Parameters["@stringVar"].SqlDbType, Is.EqualTo(SqlDbType.NVarChar));
            Assert.That(command.Parameters["@stringVar"].Value, Is.EqualTo("test"));

            Assert.That(command.Parameters["@int32Var"].SqlDbType, Is.EqualTo(SqlDbType.Int));
            Assert.That(command.Parameters["@int32Var"].Value, Is.EqualTo(123));

            Assert.That(command.Parameters["@int64Var"].SqlDbType, Is.EqualTo(SqlDbType.BigInt));
            Assert.That(command.Parameters["@int64Var"].Value, Is.EqualTo(9223372036854775807L));

            Assert.That(command.Parameters["@doubleVar"].SqlDbType, Is.EqualTo(SqlDbType.Float));
            Assert.That(command.Parameters["@doubleVar"].Value, Is.EqualTo(123.45));

            Assert.That(command.Parameters["@datetimeVar"].SqlDbType, Is.EqualTo(SqlDbType.DateTime));
            Assert.That(command.Parameters["@datetimeVar"].Value, Is.EqualTo(new DateTime(2023, 1, 1)));

            Assert.That(command.Parameters["@boolVar"].SqlDbType, Is.EqualTo(SqlDbType.Bit));
            Assert.That(command.Parameters["@boolVar"].Value, Is.EqualTo(true));
        }

        [Test]
        public void AddMemoryVariable_ShouldUseValueFromPreviousSync_WhenAvailable()
        {
            // Arrange
            var command = new SqlCommand();

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
            // Arrange
            var command = new SqlCommand();

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
