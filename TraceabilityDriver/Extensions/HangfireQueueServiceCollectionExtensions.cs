using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Hangfire.SqlServer;
using MongoDB.Driver;
using TraceabilityDriver.Models.Queues;
using TraceabilityDriver.Services.Queues;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Dependency-injection registrations for the Hangfire-backed background job queues.
    /// </summary>
    /// <remarks>
    /// This is the only place besides <see cref="HangfireTracebackQueue"/> that knows Hangfire exists.
    /// The Hangfire server runs in-process as a hosted service and creates its own storage schema on
    /// first start (SQL Server: the [HangFire] schema; Mongo: hangfire.* collections), so the storage
    /// account needs DDL rights on first run. Job classes are activated through the DI container with
    /// a scope per job, so scoped services resolve correctly inside jobs.
    /// </remarks>
    public static class HangfireQueueServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Hangfire job queues, the in-process Hangfire server, and the traceback queue services.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="config">The queue storage configuration. Must carry a connection string.</param>
        /// <returns>The same service collection, for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when the configuration has no connection string, or a Mongo store has no resolvable database name.</exception>
        public static IServiceCollection AddHangFireQueues(this IServiceCollection services, QueueConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.ConnectionString))
            {
                throw new ArgumentException("A queue connection string is required.", nameof(config));
            }

            services.AddHangfire(configuration =>
            {
                configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                             .UseSimpleAssemblyNameTypeSerializer()
                             .UseRecommendedSerializerSettings();

                switch (config.StoreType)
                {
                    case QueueStoreType.SqlServer:
                        configuration.UseSqlServerStorage(config.ConnectionString, new SqlServerStorageOptions
                        {
                            PrepareSchemaIfNecessary = true,
                            QueuePollInterval = TimeSpan.Zero,
                            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                            UseRecommendedIsolationLevel = true,
                            DisableGlobalLocks = true
                        });
                        break;

                    case QueueStoreType.Mongo:
                        // The database name can ride along in the connection string URL; an explicit
                        // config value wins so deployments with bare connection strings still work.
                        string databaseName = config.DatabaseName
                            ?? MongoUrl.Create(config.ConnectionString).DatabaseName
                            ?? throw new ArgumentException("A Mongo database name is required for the Hangfire queue store.", nameof(config));
                        configuration.UseMongoStorage(config.ConnectionString, databaseName, new MongoStorageOptions
                        {
                            CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection,
                            MigrationOptions = new MongoMigrationOptions
                            {
                                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                                BackupStrategy = new CollectionMongoBackupStrategy()
                            }
                        });
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(config), $"Unsupported queue store type: {config.StoreType}");
                }
            });

            services.AddHangfireServer();

            services.AddScoped<TracebackJob>();
            services.AddScoped<ITracebackQueue, HangfireTracebackQueue>();

            return services;
        }

        /// <summary>
        /// Registers the Hangfire job queues backed by a SQL Server database.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="connectionString">The SQL Server connection string for the queue storage.</param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection AddSqlServerHangFireQueues(this IServiceCollection services, string connectionString)
        {
            return services.AddHangFireQueues(new QueueConfig { StoreType = QueueStoreType.SqlServer, ConnectionString = connectionString });
        }

        /// <summary>
        /// Registers the Hangfire job queues backed by a MongoDB database.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="connectionString">The MongoDB connection string for the queue storage.</param>
        /// <param name="databaseName">The database to store jobs in; when null, parsed from the connection string.</param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection AddMongoHangFireQueues(this IServiceCollection services, string connectionString, string? databaseName = null)
        {
            return services.AddHangFireQueues(new QueueConfig { StoreType = QueueStoreType.Mongo, ConnectionString = connectionString, DatabaseName = databaseName });
        }
    }
}
