using Microsoft.Extensions.Configuration;
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
            return new MongoDBService(configuration);
        }
    }
}
