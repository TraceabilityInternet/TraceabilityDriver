using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTraceability.Models.Events;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.Mapping;

namespace TraceabilityDriver.Tests.Services
{
    [TestFixture]
    public class SyncService_MySqlConnector_Tests
    {
        private string _mySqlConnectionString = "server=127.0.0.1;port=3307;database=TraceabilityDriverTestDB;user=root;password=YourStrong!Passw0rd;";
        private bool _skipTests;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _skipTests = Environment.GetEnvironmentVariable("NO_SQL_DB")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

            if (_skipTests)
            {
                Assert.Ignore("Skipping MySQL synchronization tests because NO_SQL_DB environment variable is set to true");
                return;
            }

            // Setup test database with sample data
            SetupTestDatabase();
        }

        private void SetupTestDatabase()
        {
            using (var connection = new MySqlConnection(_mySqlConnectionString.Replace("database=TraceabilityDriverTestDB;", "database=mysql;")))
            {
                connection.Open();

                // Drop and recreate test database
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        DROP DATABASE IF EXISTS TraceabilityDriverTestDB;
                        CREATE DATABASE TraceabilityDriverTestDB;";
                    cmd.ExecuteNonQuery();
                }
            }

            // Create test table and insert sample data
            using (var connection = new MySqlConnection(_mySqlConnectionString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE Events (
                            Id INT PRIMARY KEY AUTO_INCREMENT,
                            EventType VARCHAR(50),
                            EventTime DATETIME,
                            OperatorId VARCHAR(50),
                            OperatorName VARCHAR(100),
                            LocationId VARCHAR(50),
                            LocationName VARCHAR(100),
                            ProductId VARCHAR(50),
                            ProductName VARCHAR(100),
                            Quantity DECIMAL(18,2),
                            UoM VARCHAR(10)
                        );

                        INSERT INTO Events (EventType, EventTime, OperatorId, OperatorName, LocationId, LocationName, ProductId, ProductName, Quantity, UoM)
                        VALUES 
                            ('gdstfishingevent', '2024-01-01 10:00:00', 'OP001', 'Operator One', 'LOC001', 'Location One', 'PROD001', 'Tuna', 100.5, 'KGM'),
                            ('gdstfishingevent', '2024-01-02 11:00:00', 'OP002', 'Operator Two', 'LOC002', 'Location Two', 'PROD002', 'Salmon', 200.75, 'KGM'),
                            ('gdstfishingevent', '2024-01-03 12:00:00', 'OP003', 'Operator Three', 'LOC003', 'Location Three', 'PROD003', 'Cod', 150.25, 'KGM');";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        [Test]
        public async Task SynchronizeAsync_WithLastIDMemoryVariable_ShouldSyncCorrectly()
        {
            if (_skipTests)
            {
                Assert.Ignore("Skipping test");
                return;
            }

            // Arrange
            var mockLogger = new Mock<ILogger<SynchronizeService>>();
            var mockConnectorFactory = new Mock<ITDConnectorFactory>();
            var mockEventsMerger = new Mock<IEventsMergerService>();
            var mockEventsConverter = new Mock<IEventsConverterService>();
            var mockDatabaseService = new Mock<IDatabaseService>();
            var mockMappingSource = new Mock<IMappingSource>();
            var syncContext = new TraceabilityDriver.Services.SynchronizationContext();

            // Setup connector to return real MySQL connector
            var mockConnectorLogger = new Mock<ILogger<TDMySqlConnector>>();
            var mockTableMappingService = new Mock<IEventsTableMappingService>();

            var connector = new TDMySqlConnector(
                mockConnectorLogger.Object,
                mockTableMappingService.Object,
                syncContext
            );

            // Setup mapping configuration
            var mappingConfig = new TDMappingConfiguration
            {
                Connections = new Dictionary<string, TDConnectorConfiguration>
                {
                    {
                        "TestDB",
                        new TDConnectorConfiguration
                        {
                            Database = "TraceabilityDriverTestDB",
                            ConnectionString = _mySqlConnectionString,
                            ConnectorType = ConnectorType.MySql,
                        }
                    }
                },
                Mappings = new List<TDMapping>
                {
                    new TDMapping
                    {
                        EventType = "gdstfishingevent",
                        Selectors = new List<TDMappingSelector>
                        {
                            new TDMappingSelector
                            {
                                Database = "TestDB",
                                Count = "SELECT COUNT(*) FROM Events WHERE Id > @LastID",
                                Selector = "SELECT * FROM Events WHERE Id > @LastID ORDER BY Id ASC LIMIT @limit OFFSET @offset",
                                Memory = new Dictionary<string, TDMappingSelectorMemoryVariable>
                                {
                                    {
                                        "LastID",
                                        new TDMappingSelectorMemoryVariable
                                        {
                                            DefaultValue = "0",
                                            Field = "$Id",
                                            DataType = "Int32"
                                        }
                                    }
                                },
                                EventMapping = new TDEventMapping()
                            }
                        }
                    }
                }
            };

            // Setup mock table mapping service to return CommonEvents
            mockTableMappingService
                .Setup(m => m.MapEvents(It.IsAny<TDEventMapping>(), It.IsAny<System.Data.DataTable>(), It.IsAny<CancellationToken>()))
                .Returns<TDEventMapping, System.Data.DataTable, CancellationToken>((mapping, dt, ct) =>
                {
                    var events = new List<CommonEvent>();
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        events.Add(new CommonEvent
                        {
                            EventId = row["Id"].ToString(),
                            EventType = row["EventType"].ToString(),
                            EventTime = (DateTime)row["EventTime"],
                            Products = new List<CommonProduct>
                            {
                                new CommonProduct
                                {
                                    ProductId = row["ProductId"].ToString(),
                                    ProductType = EventProductType.Reference,
                                    Quantity = Convert.ToDouble(row["Quantity"]),
                                    UoM = "kg",
                                    ProductDefinition = new CommonProductDefinition
                                    {
                                        ProductDefinitionId = row["ProductId"].ToString(),
                                        ShortDescription = row["ProductName"].ToString()
                                    }
                                }
                            }
                        });
                    }
                    return events;
                });

            // Setup connector factory
            mockConnectorFactory
                .Setup(f => f.CreateConnector(It.IsAny<TDConnectorConfiguration>()))
                .Returns(connector);

            // Setup mapping source
            mockMappingSource
                .Setup(m => m.GetMappings())
                .Returns(new List<TDMappingConfiguration> { mappingConfig });

            // Setup events merger
            mockEventsMerger
                .Setup(m => m.MergeEventsAsync(It.IsAny<TDMapping>(), It.IsAny<List<CommonEvent>>()))
                .ReturnsAsync((TDMapping mapping, List<CommonEvent> events) => events);

            // Setup events converter
            mockEventsConverter
                .Setup(c => c.ConvertEventsAsync(It.IsAny<List<CommonEvent>>()))
                .ReturnsAsync((List<CommonEvent> events) =>
                {
                    var doc = new EPCISDocument();
                    doc.EPCISVersion = EPCISVersion.V2;
                    doc.CreationDate = DateTimeOffset.UtcNow;
                    return doc;
                });

            List<SyncHistoryItem> syncHistory = new List<SyncHistoryItem>();

            // Setup database service
            mockDatabaseService
                .Setup(d => d.GetLatestSyncs(It.IsAny<int>()))
                .ReturnsAsync(syncHistory);

            mockDatabaseService
                .Setup(d => d.StoreSyncHistory(It.IsAny<SyncHistoryItem>()))
                .Callback<SyncHistoryItem>(syncItem =>
                {
                    syncHistory.Add(syncItem);
                })
                .Returns(Task.CompletedTask);

            mockDatabaseService
                .Setup(d => d.StoreEventsAsync(It.IsAny<List<OpenTraceability.Interfaces.IEvent>>()))
                .Returns(Task.CompletedTask);

            mockDatabaseService
                .Setup(d => d.StoreMasterDataAsync(It.IsAny<List<OpenTraceability.Interfaces.IVocabularyElement>>()))
                .Returns(Task.CompletedTask);

            var synchronizeService = new SynchronizeService(
                mockLogger.Object,
                mockConnectorFactory.Object,
                mockEventsMerger.Object,
                mockEventsConverter.Object,
                mockDatabaseService.Object,
                mockMappingSource.Object,
                syncContext
            );

            // Act - First Sync (should sync all 3 records)
            await synchronizeService.SynchronizeAsync(CancellationToken.None);

            // Assert - First Sync
            Assert.That(syncContext.CurrentSync.Status, Is.EqualTo(SyncStatus.Completed));
            Assert.That(syncContext.CurrentSync.Memory.ContainsKey("LastID"), Is.True);
            Assert.That(syncContext.CurrentSync.Memory["LastID"], Is.EqualTo("3")); // Last ID should be 3

            // Verify events were processed
            mockTableMappingService.Verify(m => m.MapEvents(
                It.IsAny<TDEventMapping>(),
                It.IsAny<System.Data.DataTable>(),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);

            // Sync again with no new records
            await synchronizeService.SynchronizeAsync(CancellationToken.None);
            Assert.That(syncContext.CurrentSync.Status, Is.EqualTo(SyncStatus.Completed));
            Assert.That(syncContext.CurrentSync.Memory.ContainsKey("LastID"), Is.True);
            Assert.That(syncContext.CurrentSync.Memory["LastID"], Is.EqualTo("3")); // Last ID should still be 3

            // Add more records to the database
            using (var connection = new MySqlConnection(_mySqlConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO Events (EventType, EventTime, OperatorId, OperatorName, LocationId, LocationName, ProductId, ProductName, Quantity, UoM)
                        VALUES 
                            ('gdstfishingevent', '2024-01-04 13:00:00', 'OP004', 'Operator Four', 'LOC004', 'Location Four', 'PROD004', 'Haddock', 175.50, 'KGM'),
                            ('gdstfishingevent', '2024-01-05 14:00:00', 'OP005', 'Operator Five', 'LOC005', 'Location Five', 'PROD005', 'Mackerel', 225.00, 'KGM');";
                    cmd.ExecuteNonQuery();
                }
            }

            // Setup for third sync - return previous sync history
            mockDatabaseService
                .Setup(d => d.GetLatestSyncs(It.IsAny<int>()))
                .ReturnsAsync(new List<SyncHistoryItem> { syncContext.CurrentSync });

            // Reset sync context for third sync
            syncContext.PreviousSync = syncContext.CurrentSync;
            syncContext.CurrentSync = new SyncHistoryItem();

            // Act - Third Sync (should only sync new records with Id > 3)
            await synchronizeService.SynchronizeAsync(CancellationToken.None);

            // Assert - Third Sync
            Assert.That(syncContext.CurrentSync.Status, Is.EqualTo(SyncStatus.Completed));
            Assert.That(syncContext.CurrentSync.Memory.ContainsKey("LastID"), Is.True);
            Assert.That(syncContext.CurrentSync.Memory["LastID"], Is.EqualTo("5")); // Last ID should now be 5

            // Verify sync history was stored (3 syncs total)
            mockDatabaseService.Verify(d => d.StoreSyncHistory(It.IsAny<SyncHistoryItem>()), Times.Exactly(3));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_skipTests) return;

            // Cleanup test database
            try
            {
                using (var connection = new MySqlConnection(_mySqlConnectionString.Replace("database=TraceabilityDriverTestDB;", "database=mysql;")))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DROP DATABASE IF EXISTS TraceabilityDriverTestDB;";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }
    }
}
