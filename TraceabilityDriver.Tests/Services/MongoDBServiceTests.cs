using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using NUnit.Framework;
using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Models.MasterData;
using OpenTraceability.Queries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Services;
using Newtonsoft.Json;
using OpenTraceability.GDST;
using OpenTraceability.Mappers;

namespace TraceabilityDriver.Tests.Services
{
    [TestFixture]
    public class MongoDBServiceTests
    {
        private MongoDBService _mongoDBService = null!;
        private IConfiguration _configuration = null!;
        private EPCISDocument _testEPCISDocument = null!;
        private bool _skipTests = false;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Check if tests should be skipped
            string skipMongoDBTests = Environment.GetEnvironmentVariable("NO_MONGO_DB") ?? string.Empty;
            _skipTests = skipMongoDBTests.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            
            if (_skipTests)
            {
                Assert.Ignore("MongoDB tests skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            OpenTraceability.GDST.Setup.Initialize();

            // Load configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            // Create a configuration with test collection names
            var testConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            // Create service with test configuration
            _mongoDBService = new MongoDBService(testConfig);

            // Load test data
            await LoadTestDataAsync();
        }

        private async Task LoadTestDataAsync()
        {
            if (_skipTests) return;

            // Load test data from JSON file
            string testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "testdata001.json");
            if (!File.Exists(testDataPath))
            {
                Assert.Fail($"Test data file not found at {testDataPath}");
            }

            string jsonData = File.ReadAllText(testDataPath);

            // deserialize the test data into an EPCISDocument
            _testEPCISDocument = OpenTraceabilityMappers.EPCISDocument.JSON.Map(jsonData);

            // save all the events into the mongo db service
            await _mongoDBService.StoreEventsAsync(_testEPCISDocument.Events);

            // save all the master data into the mongo db service
            await _mongoDBService.StoreMasterDataAsync(_testEPCISDocument.MasterData);
        }

        [Test]
        public void StoreEventsAsync_ShouldStoreEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
            }
            // This should pass via the setup method.
        }

        [Test]
        public void StoreMasterDataAsync_ShouldStoreMasterData()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
            }
            // This should pass via the setup method.
        }

        [Test]
        public async Task QueryEvents_WithTimeRange_ShouldReturnMatchingEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Determine start and end times via the _testEPCISDocument.
            var startTime = _testEPCISDocument.Events.Min(e => e.EventTime);
            var endTime = _testEPCISDocument.Events.Max(e => e.EventTime);
            
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery
                {
                    GE_eventTime = startTime,
                    LE_eventTime = endTime
                }
            };

            // Act
            var result = await _mongoDBService.QueryEvents(queryParams);

            // Assert
            Assert.That(result.Events, Is.Not.Null);
            Assert.That(result.Events.Count, Is.GreaterThan(0));
            Assert.That(result.Events.All(e => e.EventTime >= startTime && e.EventTime <= endTime), Is.True);
        }

        [Test]
        public async Task QueryEvents_WithEPC_ShouldReturnMatchingEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Arrange            
            var testEPC = _testEPCISDocument.Events.FirstOrDefault(e => e.Products.Count > 1)?.Products.First().EPC.ToString();
            Assert.That(testEPC, Is.Not.Null, "Test data should contain at least one event with an EPC");
            
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery
                {
                    MATCH_anyEPC = new List<string> { testEPC! }
                }
            };

            // Act
            var result = await _mongoDBService.QueryEvents(queryParams);

            // Assert
            Assert.That(result.Events, Is.Not.Null);
            Assert.That(result.Events, Is.Not.Empty);
            Assert.That(result.Events.Any(e => e.Products.Any(p => p.EPC.ToString() == testEPC)), Is.True);
        }

        [Test]
        public async Task QueryEvents_WithBizStep_ShouldReturnMatchingEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            await _mongoDBService.StoreEventsAsync(_testEPCISDocument.Events);
            
            // Get a business step from the test data
            var testBizStep = _testEPCISDocument.Events.First().BusinessStep;
            
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery
                {
                    EQ_bizStep = new List<string> { testBizStep.ToString() }
                }
            };

            // Act
            var result = await _mongoDBService.QueryEvents(queryParams);

            // Assert
            Assert.That(result.Events, Is.Not.Null);
            Assert.That(result.Events, Is.Not.Empty);
            Assert.That(result.Events.All(e => e.BusinessStep.ToString() == testBizStep.ToString()), Is.True);
        }

        [Test]
        public async Task QueryMasterData_ShouldReturnMatchingElement()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_MONGO_DB environment variable set to TRUE");
                return;
            }

            // Arrange
            await _mongoDBService.StoreMasterDataAsync(_testEPCISDocument.MasterData);
            
            // Get an ID from the test data
            var testElementId = _testEPCISDocument.MasterData.First().ID;

            // Act
            var result = await _mongoDBService.QueryMasterData(testElementId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ID, Is.EqualTo(testElementId));
        }
    }
} 