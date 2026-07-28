using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Tests for the migration-based schema initialization in <see cref="SqlServerService"/>.
    /// </summary>
    /// <remarks>
    /// These tests prove the two upgrade paths a real deployment can hit: a fresh database built entirely
    /// by migrations, and an existing database created by the old EnsureCreated path (tables present, no
    /// migrations history) that must be baselined and then migrated forward without erroring on existing
    /// tables. The database is dropped and rebuilt by each test, so the fixture is ordered and standalone.
    /// </remarks>
    [TestFixture]
    [Category("AdvancedTest")]
    [NonParallelizable]
    public class SqlServerServiceMigrationTests
    {
        private const string InitialSchemaMigrationId = "20260723205038_InitialSchema";

        private IDbContextFactory<ApplicationDbContext> _contextFactory = null!;
        private SqlServerService _dbService = null!;
        private bool _skipTests = false;

        /// <summary>
        /// Builds the service and context factory against the test database.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            string skipSqlTests = Environment.GetEnvironmentVariable("NO_SQL_DB") ?? string.Empty;
            _skipTests = skipSqlTests.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

            if (_skipTests)
            {
                Assert.Ignore("SqlServer tests skipped due to NO_SQL_DB environment variable set to TRUE");
                return;
            }

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            string connectionString = configuration["SqlServer:ConnectionString"] ?? throw new Exception("SqlServer connection string not configured");
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString, x => x.EnableRetryOnFailure())
                .Options;

            _contextFactory = new PooledDbContextFactory<ApplicationDbContext>(options);
            _dbService = new SqlServerService(new LoggerFactory().CreateLogger<SqlServerService>(), _contextFactory, configuration);
        }

        /// <summary>
        /// Skips the current test when the backend is unavailable.
        /// </summary>
        private void SkipIfUnavailable()
        {
            if (_skipTests)
            {
                Assert.Ignore("Test skipped due to NO_SQL_DB environment variable set to TRUE");
            }
        }

        /// <summary>
        /// A fresh database must be built entirely through the migration pipeline, including the traceback tables.
        /// </summary>
        [Test]
        [Order(1)]
        public async Task InitializeDatabase_FreshDatabase_AppliesAllMigrations()
        {
            SkipIfUnavailable();

            // Arrange
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.Database.EnsureDeletedAsync();

            // Act
            await _dbService.InitializeDatabase();

            // Assert
            List<string> appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.That(appliedMigrations, Does.Contain(InitialSchemaMigrationId));
            Assert.That(appliedMigrations.Count, Is.GreaterThanOrEqualTo(2), "Both the initial schema and the traceback migration must be applied.");

            // The traceback tables must be queryable.
            Assert.That(await context.Tracebacks.CountAsync(), Is.EqualTo(0));
            Assert.That(await context.TracebackItems.CountAsync(), Is.EqualTo(0));
        }

        /// <summary>
        /// A database created by the old EnsureCreated path (schema present, no migrations history) must be
        /// baselined and then migrated forward without erroring on the tables that already exist.
        /// </summary>
        [Test]
        [Order(2)]
        public async Task InitializeDatabase_PreMigrationsDatabase_BaselinesAndMigratesForward()
        {
            SkipIfUnavailable();

            // Arrange - simulate an existing deployment: only the initial schema exists and there is no
            // migrations history, exactly what EnsureCreated left behind before migrations were adopted.
            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                await context.Database.EnsureDeletedAsync();

                IMigrator migrator = context.Database.GetService<IMigrator>();
                await migrator.MigrateAsync(InitialSchemaMigrationId);

                await context.Database.ExecuteSqlRawAsync("DROP TABLE [__EFMigrationsHistory];");
            }

            // Act
            await _dbService.InitializeDatabase();

            // Assert
            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                List<string> appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();
                Assert.That(appliedMigrations, Does.Contain(InitialSchemaMigrationId), "The pre-existing schema must be baselined, not re-created.");
                Assert.That(appliedMigrations.Count, Is.GreaterThanOrEqualTo(2), "The traceback migration must be applied on top of the baseline.");

                // The traceback tables must have been added to the existing database.
                Assert.That(await context.Tracebacks.CountAsync(), Is.EqualTo(0));
                Assert.That(await context.TracebackItems.CountAsync(), Is.EqualTo(0));
            }
        }

        /// <summary>
        /// Running the initialization twice must be a harmless no-op.
        /// </summary>
        [Test]
        [Order(3)]
        public async Task InitializeDatabase_RunTwice_IsIdempotent()
        {
            SkipIfUnavailable();

            // Act
            await _dbService.InitializeDatabase();
            await _dbService.InitializeDatabase();

            // Assert
            using var context = await _contextFactory.CreateDbContextAsync();
            Assert.That((await context.Database.GetPendingMigrationsAsync()), Is.Empty);
        }
    }
}
