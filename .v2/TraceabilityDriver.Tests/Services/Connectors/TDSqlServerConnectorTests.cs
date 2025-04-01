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
        private readonly TDSqlServerConnector _connector;

        public TDSqlServerConnectorTests()
        {
            _configuration = new TDConnectorConfiguration();
            _mockOptions = new Mock<IOptions<TDConnectorConfiguration>>();
            _mockOptions.Setup(o => o.Value).Returns(_configuration);

            _mockLogger = new Mock<ILogger<TDSqlServerConnector>>();
            _mockEventsTableMappingService = new Mock<IEventsTableMappingService>();
            _connector = new TDSqlServerConnector(
                _mockLogger.Object,
                _mockEventsTableMappingService.Object);
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
                EventMapping = new TDEventMapping()
            };

            var dataTable = new DataTable();
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Rows.Add(1, "Event1");

            _configuration.ConnectionString = _connectionString.Replace("=master;", "=TestEventsDatabase;");
            _mockEventsTableMappingService.Setup(m => m.MapEvents(selector.EventMapping, It.Is<DataTable>(d => d.Rows.Count > 0), It.IsAny<CancellationToken>()))
                .Returns(new List<CommonEvent> { new CommonEvent { EventId = "123", EventType = "Fishing" } });

            // Act
            var events = await _connector.GetEventsAsync(_configuration, selector, CancellationToken.None, null);

            // Assert
            Assert.That(events, Is.Not.Null);
            Assert.That(events.Count(), Is.EqualTo(1));
            Assert.That(events.First().EventId, Is.EqualTo("123"));
            Assert.That(events.First().EventType, Is.EqualTo("Fishing"));
        }

        [Test]
        public void GetEventsAsync_ShouldThrowException_WhenQueryFails()
        {
            // Arrange
            var selector = new TDMappingSelector
            {
                Selector = "SELECT * FROM InvalidTable",
                EventMapping = new TDEventMapping()
            };

            _configuration.ConnectionString = _connectionString.Replace("=master;", "=TestEventsDatabase;");

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => _connector.GetEventsAsync(_configuration, selector, CancellationToken.None, null));
        }
    }
}
