using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace TraceabilityDriver.Tests.TestDatabase
{
    public interface ITestDatabase
    {
        void SetupDatabase();
    }

    public class TestMSSQLDatabase : ITestDatabase
    {
        private readonly TestDatabaseConfig _config;

        public TestMSSQLDatabase(TestDatabaseConfig config)
        {
            _config = config;
        }

        public void SetupDatabase()
        {
            // make sure you connect to the server, not the specific database
            string serverConnectionString = _config.ConnectionString.Replace($"={_config.DatabaseName};", "=master;");
            using var connection = new SqlConnection(serverConnectionString);
            connection.Open();

            // Drop and recreate test database
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @$"
                        IF EXISTS(SELECT * FROM sys.databases WHERE name = '{_config.DatabaseName}')
                        BEGIN
                            ALTER DATABASE {_config.DatabaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                            DROP DATABASE {_config.DatabaseName};
                        END
                        CREATE DATABASE {_config.DatabaseName};";
                cmd.ExecuteNonQuery();
            }

            // Add required tables to db
            Build();

            // Seed initial data
            SeedData();
        }

        private void Build()
        {
            ExecuteCommand(_config.BuildCommand);
        }

        private void SeedData()
        {
            ExecuteCommand(_config.SeedCommand);
        }

        private void ExecuteCommand(string command)
        {
            // create a sql client
            using var connection = new SqlConnection(_config.ConnectionString);
            connection.Open();

            // Split the command into batches based on "GO" statements
            var batches = SplitSqlBatches(command);
            foreach (var batch in batches)
            {
                using var sqlCommand = new SqlCommand(batch, connection);
                sqlCommand.ExecuteNonQuery();
            }
        }

        private static IEnumerable<string> SplitSqlBatches(string sql)
        {
            // Split on lines that contain only "GO" (case-insensitive), allowing whitespace.
            var batches = Regex.Split(
                sql,
                @"^\s*GO\s*(?:--.*)?$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase
            );

            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    yield return trimmed;
            }
        }
    }
}
