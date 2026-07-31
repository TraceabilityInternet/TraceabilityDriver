using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Event query filter tests for <see cref="SqlServerService"/>.
    /// </summary>
    [TestFixture]
    [Category("AdvancedTest")]
    public class SqlServerServiceQueryTests : DatabaseServiceQueryTestsBase
    {
        /// <inheritdoc/>
        protected override string SkipEnvironmentVariable => "NO_SQL_DB";

        /// <inheritdoc/>
        protected override IDatabaseService CreateService(IConfiguration configuration)
        {
            string connectionString = configuration["SqlServer:ConnectionString"] ?? throw new Exception("SqlServer connection string not configured");
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString, x => x.EnableRetryOnFailure())
                .Options;

            var contextFactory = new PooledDbContextFactory<ApplicationDbContext>(options);
            ILogger<SqlServerService> logger = new LoggerFactory().CreateLogger<SqlServerService>();
            return new SqlServerService(logger, contextFactory, configuration);
        }
    }
}
