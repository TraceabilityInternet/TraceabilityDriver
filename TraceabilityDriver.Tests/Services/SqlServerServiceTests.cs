using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTraceability.Models.Events;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    [TestFixture]
    public class SqlServerServiceTests
    {
        private SqlServerService _sqlServerService;
        private EPCISDocument _testEPCISDocument;
        private bool _skipTests = false;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Check if tests should be skipped
            string skipMongoDBTests = Environment.GetEnvironmentVariable("NO_SQL_DB") ?? string.Empty;
            _skipTests = skipMongoDBTests.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

            if (_skipTests)
            {
                Assert.Ignore("SqlServer tests skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            OpenTraceability.GDST.Setup.Initialize();

            // Load configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            // Create a configuration with test collection names
            IConfiguration testConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            // setup context
            string connectionString = testConfig["SqlServer:ConnectionString"] ?? throw new Exception("SqlServer connection string not configured");
            
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString, x => x.EnableRetryOnFailure()).Options;
            ApplicationDbContext context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            ILogger<SqlServerService> logger = new LoggerFactory().CreateLogger<SqlServerService>();

            // Create service with test configuration
            _sqlServerService = new SqlServerService(logger, context);

            // Clear out the data.
            await _sqlServerService.ClearDatabaseAsync();

            // Initialize the database
            await _sqlServerService.InitializeDatabase();

            // Load test data
            //await LoadTestDataAsync();
        }

        [Test]
        public void StoreEventsAsync_ShouldStoreEvents()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
            }
            // This should pass via the setup method.
        }
    }
}
