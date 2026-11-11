using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTraceability.Models.Events;
using System.Text.Json;
using TraceabilityDriver.Models.Sql;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services.LegacyCacheUpgrade
{
    /// <summary>
    /// Upgrade tests for <see cref="SqlServerService"/> against a cache written by the last driver build before the bizLocation column.
    /// </summary>
    /// <remarks>
    /// The snapshot rows are restored into a dedicated database whose schema is then reduced to the legacy shape (no BizLocationGLN
    /// column or index), which is the only schema difference from the current model. <see cref="SqlServerService.InitializeDatabase"/>
    /// then performs the real upgrade: DDL plus backfill.
    /// </remarks>
    [TestFixture]
    [Category("AdvancedTest")]
    public class SqlServerLegacyCacheUpgradeTests
    {
        private const string DatabaseName = "TraceabilityDriverLegacyUpgradeTests";
        private const string EventsSnapshotFile = "SqlEPCISEvents_PreBizLocation.json";
        private const string SearchRowsSnapshotFile = "SqlEventSearchDocuments_PreBizLocation.json";
        private const string RestrictedLogin = "td_legacy_upgrade_readwrite";
        private const string RestrictedPassword = "Upgrade!Test#2026x";

        private bool _skipTests;
        private string _connectionString = null!;
        private IDbContextFactory<ApplicationDbContext> _contextFactory = null!;
        private SqlServerService _service = null!;
        private List<EventSearchSqlDocument> _legacySearchRows = null!;
        private EPCISQueryDocument _storedEvents = null!;
        private Dictionary<string, List<string>> _resultsBeforeUpgrade = null!;

        /// <summary>
        /// Restores the legacy snapshot, records non-location query results, then runs the startup upgrade once.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _skipTests = (Environment.GetEnvironmentVariable("NO_SQL_DB") ?? string.Empty).Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            if (_skipTests)
            {
                Assert.Ignore("SqlServer tests skipped due to NO_SQL_DB environment variable set to TRUE");
            }

            OpenTraceability.GDST.Setup.Initialize();

            IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.Tests.json").Build();
            string baseConnectionString = configuration["SqlServer:ConnectionString"] ?? throw new Exception("SqlServer connection string not configured");
            _connectionString = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = DatabaseName }.ConnectionString;
            _contextFactory = CreateContextFactory(_connectionString);

            List<EPCISEventSqlDocument> legacyEventRows = JsonSerializer.Deserialize<List<EPCISEventSqlDocument>>(LegacyCacheData.ReadFile(EventsSnapshotFile))!;
            _legacySearchRows = JsonSerializer.Deserialize<List<EventSearchSqlDocument>>(LegacyCacheData.ReadFile(SearchRowsSnapshotFile))!;
            await CreateLegacyDatabaseAsync(legacyEventRows, _legacySearchRows);

            _storedEvents = LegacyCacheData.ParseStoredEvents(legacyEventRows.Select(r => r.EventJson));
            _service = new SqlServerService(NullLogger<SqlServerService>.Instance, _contextFactory);
            _resultsBeforeUpgrade = await LegacyCacheData.CaptureResultsAsync(_service, LegacyCacheData.BuildNonLocationQueries(_storedEvents));

            await _service.InitializeDatabase();
        }

        /// <summary>
        /// Drops the database and login created for this fixture.
        /// </summary>
        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_skipTests)
            {
                return;
            }

            using (ApplicationDbContext context = await _contextFactory.CreateDbContextAsync())
            {
                await context.Database.EnsureDeletedAsync();
            }

            using SqlConnection connection = new SqlConnection(new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = "master" }.ConnectionString);
            await connection.OpenAsync();
            using SqlCommand dropLogin = new SqlCommand($"IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{RestrictedLogin}') DROP LOGIN {RestrictedLogin};", connection);
            await dropLogin.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// The snapshot must contain GLNs where legacy broad matching was wrong, otherwise these tests could not detect the bug.
        /// </summary>
        [Test]
        public void LegacySnapshot_BroadLocationMatching_DiffersFromLibraryFilter()
        {
            // Arrange
            Dictionary<string, List<string>> legacyLocationGLNs = _legacySearchRows.GroupBy(r => r.EventId).ToDictionary(g => g.Key, g => g.Select(r => r.LocationGLN.ToLower()).Where(l => l.Length > 0).ToList());

            // Act
            int differingGLNs = LegacyCacheData.CountGLNsWhereLegacyMatchingDiffers(_storedEvents, legacyLocationGLNs);

            // Assert
            Assert.That(differingGLNs, Is.GreaterThan(0), "The snapshot should include GLNs used as source/destination locations of events at other bizLocations.");
        }

        /// <summary>
        /// Every legacy search row receives its event's business location, parsed from the stored event JSON.
        /// </summary>
        [Test]
        public async Task InitializeDatabase_LegacyCache_SetsBizLocationOnEverySearchRow()
        {
            // Arrange
            Dictionary<string, string> expectedByEventId = _storedEvents.Events.ToDictionary(e => e.EventID!.ToString(), e => e.Location?.GLN?.ToString().ToLower() ?? string.Empty);

            // Act
            using ApplicationDbContext context = await _contextFactory.CreateDbContextAsync();
            List<EventSearchSqlDocument> upgraded = await context.EventSearchDocuments.AsNoTracking().ToListAsync();

            // Assert
            Assert.That(upgraded, Has.Count.EqualTo(_legacySearchRows.Count));
            foreach (EventSearchSqlDocument row in upgraded)
            {
                Assert.That(row.BizLocationGLN, Is.EqualTo(expectedByEventId[row.EventId]), $"Search row {row.ID} of event {row.EventId} has the wrong bizLocation.");
            }
        }

        /// <summary>
        /// After the upgrade, EQ_bizLocation agrees with OpenTraceability's own filter for every GLN in the cache.
        /// </summary>
        [Test]
        public async Task QueryEvents_EveryGLNAfterUpgrade_MatchesLibraryFilter()
        {
            // Act
            List<string> mismatches = await LegacyCacheData.FindBizLocationMismatchesAsync(_service, _storedEvents);

            // Assert
            Assert.That(mismatches, Is.Empty, string.Join(Environment.NewLine, mismatches));
        }

        /// <summary>
        /// The upgrade must not change the results of any query that does not filter on location.
        /// </summary>
        [Test]
        public async Task QueryEvents_NonLocationFilters_UnchangedByUpgrade()
        {
            // Act
            Dictionary<string, List<string>> resultsAfterUpgrade = await LegacyCacheData.CaptureResultsAsync(_service, LegacyCacheData.BuildNonLocationQueries(_storedEvents));

            // Assert
            Assert.That(_resultsBeforeUpgrade.Values.Sum(ids => ids.Count), Is.GreaterThan(0), "The comparison queries should return events, or the check is vacuous.");
            Assert.That(resultsAfterUpgrade, Is.EqualTo(_resultsBeforeUpgrade));
        }

        /// <summary>
        /// Running the startup upgrade again changes nothing.
        /// </summary>
        [Test]
        public async Task InitializeDatabase_RunAgain_ChangesNothing()
        {
            // Arrange
            List<(long, string?)> before = await GetBizLocationsByRowAsync();

            // Act
            await _service.InitializeDatabase();

            // Assert
            Assert.That(await GetBizLocationsByRowAsync(), Is.EqualTo(before));
        }

        /// <summary>
        /// A login without ALTER permission fails startup with an error carrying the SQL a DBA must run, instead of failing later on writes.
        /// </summary>
        [Test]
        public async Task InitializeDatabase_LoginWithoutAlterPermission_ThrowsWithManualUpgradeSql()
        {
            try
            {
                // Arrange
                using (ApplicationDbContext context = await _contextFactory.CreateDbContextAsync())
                {
                    await DropBizLocationColumnAsync(context);
                    await context.Database.ExecuteSqlRawAsync($"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{RestrictedLogin}') CREATE LOGIN {RestrictedLogin} WITH PASSWORD = N'{RestrictedPassword}';");
                    await context.Database.ExecuteSqlRawAsync($"IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{RestrictedLogin}') CREATE USER {RestrictedLogin} FOR LOGIN {RestrictedLogin};");
                    await context.Database.ExecuteSqlRawAsync($"ALTER ROLE db_datareader ADD MEMBER {RestrictedLogin}; ALTER ROLE db_datawriter ADD MEMBER {RestrictedLogin};");
                }

                string restrictedConnectionString = new SqlConnectionStringBuilder(_connectionString) { UserID = RestrictedLogin, Password = RestrictedPassword }.ConnectionString;
                SqlServerService restrictedService = new SqlServerService(NullLogger<SqlServerService>.Instance, CreateContextFactory(restrictedConnectionString));

                // Act
                InvalidOperationException? exception = Assert.ThrowsAsync<InvalidOperationException>(() => restrictedService.InitializeDatabase());

                // Assert
                Assert.That(exception!.Message, Does.Contain("ALTER TABLE EventSearchDocuments ADD BizLocationGLN"));
                Assert.That(exception.Message, Does.Contain("CREATE INDEX"));
                Assert.That(exception.InnerException, Is.InstanceOf<SqlException>());
            }
            finally
            {
                // Restore the upgraded state the other tests in this fixture rely on.
                await _service.InitializeDatabase();
            }
        }

        /// <summary>
        /// Creates a context factory over the given connection string, configured like the application.
        /// </summary>
        private static IDbContextFactory<ApplicationDbContext> CreateContextFactory(string connectionString)
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString, x => x.EnableRetryOnFailure()).Options;
            return new PooledDbContextFactory<ApplicationDbContext>(options);
        }

        /// <summary>
        /// Creates a fresh database holding the snapshot rows in the legacy schema, which lacks the BizLocationGLN column and index.
        /// </summary>
        private async Task CreateLegacyDatabaseAsync(List<EPCISEventSqlDocument> eventRows, List<EventSearchSqlDocument> searchRows)
        {
            using ApplicationDbContext context = await _contextFactory.CreateDbContextAsync();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            // Rows are inserted through the current model, then the column is dropped; the result matches what the legacy build created.
            foreach (EPCISEventSqlDocument row in eventRows)
            {
                row.ID = 0;
            }
            foreach (EventSearchSqlDocument row in searchRows)
            {
                row.ID = 0;
                row.BizLocationGLN = null;
            }
            context.EPCISEvents.AddRange(eventRows);
            context.EventSearchDocuments.AddRange(searchRows);
            await context.SaveChangesAsync();

            await DropBizLocationColumnAsync(context);
        }

        /// <summary>
        /// Removes the BizLocationGLN index and column, returning the search table to its legacy shape.
        /// </summary>
        private static async Task DropBizLocationColumnAsync(ApplicationDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync($"DROP INDEX {ApplicationDbContext.BizLocationGLNIndexName} ON EventSearchDocuments;");
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE EventSearchDocuments DROP COLUMN BizLocationGLN;");
        }

        /// <summary>
        /// Reads the bizLocation of every search row, ordered by row ID.
        /// </summary>
        private async Task<List<(long, string?)>> GetBizLocationsByRowAsync()
        {
            using ApplicationDbContext context = await _contextFactory.CreateDbContextAsync();
            List<EventSearchSqlDocument> rows = await context.EventSearchDocuments.AsNoTracking().OrderBy(r => r.ID).ToListAsync();
            return rows.Select(r => (r.ID, r.BizLocationGLN)).ToList();
        }
    }
}
