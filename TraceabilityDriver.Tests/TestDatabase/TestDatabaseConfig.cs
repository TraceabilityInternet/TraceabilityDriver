using Microsoft.Data.SqlClient;

namespace TraceabilityDriver.Tests.TestDatabase
{

    public class TestDatabaseConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string BuildCommand { get; set; } = string.Empty;
        public string SeedCommand { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }
}
