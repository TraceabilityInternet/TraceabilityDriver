using Extensions;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using System.Collections.Concurrent;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Models.Sql;
using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// MS SQL Server implementation of the IDatabaseService interface, responsible for storing and querying traceability data in a SQL Server GDST data cache.
    /// </summary>
    public class SqlServerService : IDatabaseService
    {
        private readonly ILogger<SqlServerService> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public SqlServerService(ILogger<SqlServerService> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// The migration id of the initial schema snapshot, used to baseline databases created before migrations were adopted.
        /// </summary>
        private const string InitialSchemaMigrationId = "20260723205038_InitialSchema";

        /// <summary>
        /// SQL that stamps a pre-migrations database as already having the initial schema. Databases created by the old
        /// EnsureCreated path have the tables but no migrations history, so without this stamp MigrateAsync would try to
        /// re-create tables that already exist. The statement is a no-op on fresh databases and on databases that already
        /// have a migrations history.
        /// </summary>
        private const string BaselineSql = @"
IF OBJECT_ID(N'[EPCISEvents]', N'U') IS NOT NULL AND OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'" + InitialSchemaMigrationId + @"', N'10.0.10');
END";

        /// <summary>
        /// Brings the database schema up to date by applying any pending EF Core migrations.
        /// </summary>
        /// <remarks>
        /// The data cache is durable now that tracebacks pull external data into it, so the schema is evolved with
        /// migrations instead of being dropped and recreated. Databases created before migrations were adopted are
        /// baselined first (see <see cref="BaselineSql"/>), then migrated forward. Safe to run on every startup.
        /// </remarks>
        public async Task InitializeDatabase()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Baseline databases that predate migrations before applying anything. The check requires a live
                // database; when none exists yet, MigrateAsync below creates it from scratch.
                if (await context.Database.CanConnectAsync())
                {
                    await context.Database.ExecuteSqlRawAsync(BaselineSql);
                }

                await context.Database.MigrateAsync();

                _logger.LogInformation("Database schema is up to date.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        public async Task<DatabaseStoreResult> StoreEventsAsync(List<IEvent> events)
        {
            ConcurrentBag<string> createdIds = new ConcurrentBag<string>();
            ConcurrentBag<string> updatedIds = new ConcurrentBag<string>();

            List<List<IEvent>> batches = events.Batch(42); // 42 is a magic number for performance when adding entities using ef core.
            await Parallel.ForEachAsync(batches, async (batch, ct) =>
            {
                using (var context = await _contextFactory.CreateDbContextAsync())
                {
                    // rather than check for events to update in the loop below,
                    // we want to execute one large query to get all the events that will be updated rather than added
                    List<string> storingEventIds = batch.Select(x => x.EventID.ToString()).ToList();
                    var existingEvents = await context.EPCISEvents.Where(x => storingEventIds.Contains(x.EventId)).ToDictionaryAsync(x => x.EventId, y => y);

                    foreach (IEvent evt in batch)
                    {
                        EPCISEventSqlDocument doc = new EPCISEventSqlDocument(evt);
                        if (existingEvents.TryGetValue(evt.EventID.ToString(), out EPCISEventSqlDocument? existingEvent))
                        {
                            // Preserve the _id field from the existing document
                            doc.ID = existingEvent.ID;
                            context.Entry(existingEvent).CurrentValues.SetValues(doc);
                            updatedIds.Add(evt.EventID.ToString());
                        }
                        else
                        {
                            context.EPCISEvents.Add(doc);
                            createdIds.Add(evt.EventID.ToString());
                        }
                    }

                    // Now save the search model.
                    List<EventSearchSqlDocument> searchDocuments = EventSearchSqlDocument.CreateSearchDocuments(batch);

                    // Batch save the search documents by first deleting all existing index documents 
                    // for the given event IDs and then adding the new ones.
                    var existingSearchDocuments = await context.EventSearchDocuments
                    .Where(x => storingEventIds.Contains(x.EventId))
                            .ToListAsync();

                    context.EventSearchDocuments.RemoveRange(existingSearchDocuments);
                    context.EventSearchDocuments.AddRange(searchDocuments);

                    await context.SaveChangesAsync();
                }
            });

            return new DatabaseStoreResult { CreatedIds = createdIds.ToList(), UpdatedIds = updatedIds.ToList() };
        }

        public async Task<DatabaseStoreResult> StoreMasterDataAsync(List<IVocabularyElement> masterData)
        {
            DatabaseStoreResult result = new DatabaseStoreResult();

            using var context = await _contextFactory.CreateDbContextAsync();
            foreach (var element in masterData)
            {
                var masterDataDoc = new MasterDataSqlDocument
                {
                    ElementId = element.ID,
                    ElementType = element.GetType().AssemblyQualifiedName ?? "",
                    ElementJson = OpenTraceability.Mappers.OpenTraceabilityMappers.MasterData.GS1WebVocab.Map(element)
                };

                // Check if master data with this ID already exists
                var existingMasterData = await context.MasterDataDocuments.FirstOrDefaultAsync(x => x.ElementId == element.ID);

                if (existingMasterData == null)
                {
                    // Insert new master data
                    await context.AddAsync(masterDataDoc);
                    result.CreatedIds.Add(element.ID);
                }
                else
                {
                    // Preserve the _id field from the existing document
                    masterDataDoc.ID = existingMasterData.ID;

                    // Replace existing master data
                    context.Entry(existingMasterData).CurrentValues.SetValues(masterDataDoc);
                    result.UpdatedIds.Add(element.ID);
                }
            }

            await context.SaveChangesAsync();

            return result;
        }

        public async Task StoreSyncHistory(SyncHistoryItem syncHistory)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.SyncHistory.AddAsync(syncHistory);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task StoreTracebackAsync(TracebackRecord traceback)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existingRecord = await context.Tracebacks.FirstOrDefaultAsync(x => x.Id == traceback.Id);
            if (existingRecord == null)
            {
                await context.Tracebacks.AddAsync(traceback);
            }
            else
            {
                context.Entry(existingRecord).CurrentValues.SetValues(traceback);

                // SetValues only copies scalar columns; the JSON-converted list properties must be assigned explicitly.
                existingRecord.RequestedEpcs = traceback.RequestedEpcs;
                existingRecord.Errors = traceback.Errors;
            }

            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task StoreTracebackItemsAsync(List<TracebackItem> items)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            foreach (var item in items)
            {
                // Upsert by the natural key so retried ledger writes never create duplicate entries.
                var existingItem = await context.TracebackItems.FirstOrDefaultAsync(x => x.TracebackId == item.TracebackId && x.ItemType == item.ItemType && x.ItemId == item.ItemId);
                if (existingItem == null)
                {
                    await context.TracebackItems.AddAsync(item);
                }
                else
                {
                    item.Id = existingItem.Id;
                    context.Entry(existingItem).CurrentValues.SetValues(item);
                }
            }

            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<List<TracebackRecord>> GetTracebacksAsync(int top = 100, int skip = 0)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tracebacks.OrderByDescending(x => x.StartTime).Skip(skip).Take(top).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<TracebackRecord?> GetTracebackAsync(string id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tracebacks.FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc/>
        public async Task<List<TracebackItem>> GetTracebackItemsAsync(string tracebackId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TracebackItems.Where(x => x.TracebackId == tracebackId).ToListAsync();
        }

        public async Task<EPCISQueryDocument> QueryEvents(EPCISQueryParameters options)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // First, query the search documents to find matching event IDs
            var searchQuery = context.EventSearchDocuments.AsQueryable();

            // Apply query filters to the search documents
            if (options.query.MATCH_anyEPCClass.Count > 0)
            {
                List<string> prefixes = options.query.MATCH_anyEPCClass
                    .Where(epc => epc.EndsWith('*'))
                    .Select(epc => epc.Substring(0, epc.IndexOf('*')).ToLower())
                    .ToList();

                searchQuery = searchQuery.Where(e =>
                    prefixes.Any(prefix => e.EPC.StartsWith(prefix)) ||
                    options.query.MATCH_anyEPCClass.Contains(e.EPC));
            }

            if (options.query.MATCH_anyEPC.Count > 0)
            {
                searchQuery = searchQuery.Where(e => options.query.MATCH_anyEPC.Contains(e.EPC.ToLower()));
            }

            // Add time range filters
            if (options.query.GE_eventTime.HasValue)
            {
                searchQuery = searchQuery.Where(e => e.EventTime >= options.query.GE_eventTime.Value);
            }

            if (options.query.LE_eventTime.HasValue)
            {
                searchQuery = searchQuery.Where(e => e.EventTime <= options.query.LE_eventTime.Value);
            }

            // Add record time range filters
            if (options.query.GE_recordTime.HasValue)
            {
                searchQuery = searchQuery.Where(e => e.RecordTime >= options.query.GE_recordTime.Value);
            }

            if (options.query.LE_recordTime.HasValue)
            {
                searchQuery = searchQuery.Where(e => e.RecordTime <= options.query.LE_recordTime.Value);
            }

            // Add bizStep filters
            if (options.query.EQ_bizStep?.Count > 0)
            {
                searchQuery = searchQuery.Where(e => options.query.EQ_bizStep.Contains(e.BizStep));
            }

            // Add action filters
            if (options.query.EQ_action?.Count > 0)
            {
                searchQuery = searchQuery.Where(e => options.query.EQ_action.Contains(e.Action));
            }

            // Add location filters
            if (options.query.EQ_bizLocation.Count > 0)
            {
                List<string> bizLocations = options.query.EQ_bizLocation.Select(loc => loc.ToString().ToLower()).ToList();
                searchQuery = searchQuery.Where(e => bizLocations.Contains(e.LocationGLN));
            }

            // Get unique event IDs from the search results
            var matchingEventIds = await searchQuery
                .Select(e => e.EventId)
                .Distinct()
                .ToListAsync();

            // Now query the actual EPCIS events using the event IDs from search
            var events = await context.EPCISEvents
                .Where(e => matchingEventIds.Contains(e.EventId))
                .ToListAsync();

            // Convert results back to EPCIS events
            var doc = new EPCISQueryDocument
            {
                EPCISVersion = EPCISVersion.V2,
                Events = new List<IEvent>()
            };

            ConcurrentBag<EPCISQueryDocument> queryDocs = new();
            Parallel.ForEach(events, (eventItem, ct) =>
            {
                EPCISQueryDocument queryDoc = OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(eventItem.EventJson);
                queryDocs.Add(queryDoc);
            });

            foreach (var queryDoc in queryDocs)
            {
                doc.Merge(queryDoc);
            }

            return doc;
        }

        public async Task<IVocabularyElement?> QueryMasterData(string identifier)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var masterDataDoc = await context.MasterDataDocuments.FirstOrDefaultAsync(x => x.ElementId == identifier);
            if (masterDataDoc == null)
            {
                return null;
            }
            else
            {
                Type t = Type.GetType(masterDataDoc.ElementType)
                        ?? throw new Exception($"Failed to get type: {masterDataDoc.ElementType}");
                return OpenTraceabilityMappers.MasterData.GS1WebVocab.Map(t, masterDataDoc.ElementJson);
            }
        }

        public async Task<List<SyncHistoryItem>> GetLatestSyncs(int top = 10)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.SyncHistory.OrderByDescending(x => x.EndTime).Take(top).ToListAsync();
        }

        public async Task<DatabaseReport> GetDatabaseReport()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var report = new DatabaseReport();

            // Get event counts by bizStep
            var eventsBizStepGroups = await context.EPCISEvents.GroupBy(x => x.BizStep).Select(x => new { BizStep = x.Key, Count = x.Count() }).ToListAsync();

            foreach (var group in eventsBizStepGroups)
            {
                if (!string.IsNullOrEmpty(group.BizStep))
                {
                    report.EventCounts[group.BizStep] = group.Count;
                }
            }

            // Get master data counts by type
            var masterDataTypeGroups = await context.MasterDataDocuments.GroupBy(x => x.ElementType).Select(x => new { Type = x.Key, Count = x.Count() }).ToListAsync();

            foreach (var group in masterDataTypeGroups)
            {
                if (!string.IsNullOrEmpty(group.Type))
                {
                    // Get the type name from the assembly qualified name
                    Type t = Type.GetType(group.Type) ?? throw new Exception($"Failed to get type: {group.Type}");
                    report.MasterDataCounts[t.Name] = group.Count;
                }
            }

            // Get sync counts by status
            var syncStatusGroups = await context.SyncHistory.GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count() }).ToListAsync();

            foreach (var group in syncStatusGroups)
            {
                report.SyncCounts[group.Status] = group.Count;
            }

            return report;
        }

        public async Task<List<LogModel>> GetLastErrors(int top = 10)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            List<LogModelSql> logs = await context.Logs
                .Where(x => x.Level == LogLevel.Error)
                .OrderByDescending(x => x.TimeStamp)
                .Take(top)
                .ToListAsync();

            List<LogModel> logModels = logs.Select(x => new LogModel()
            {
                Id = x.Id.ToString(),
                Message = x.Message ?? string.Empty,
                Level = x.Level.ToString(),
                Timestamp = x.TimeStamp,
            }).ToList();

            return logModels;
        }

        public async Task ClearDatabaseAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                await context.Database.EnsureDeletedAsync();

                // Rebuild through the migration pipeline so a cleared database matches exactly what
                // a freshly migrated one looks like, including the migrations history.
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing database");
                throw;
            }
        }
    }
}
