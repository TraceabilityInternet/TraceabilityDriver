using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Traceback storage tests for <see cref="MongoDBService"/>.
    /// </summary>
    [TestFixture]
    [Category("AdvancedTest")]
    public class MongoDBServiceTracebackTests : DatabaseServiceTracebackTestsBase
    {
        /// <inheritdoc/>
        protected override string SkipEnvironmentVariable => "NO_MONGO_DB";

        /// <inheritdoc/>
        protected override IDatabaseService CreateService(IConfiguration configuration)
        {
            ILogger<MongoDBService> logger = new LoggerFactory().CreateLogger<MongoDBService>();
            return new MongoDBService(logger, configuration);
        }
    }
}
