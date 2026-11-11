using Extensions;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using System.Collections.Concurrent;
using TraceabilityDriver.Models;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Models.Sql;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// MS SQL Server implementation of the IDatabaseService interface, responsible for storing and querying traceability data in a SQL Server GDST data cache.
    /// </summary>
    public class SqlServerService : IDatabaseService
    {
        private readonly ILogger<SqlServerService> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        /// <summary>
        /// Idempotently adds the BizLocationGLN column to search tables created before it existed.
        /// </summary>
        private const string AddBizLocationColumnSql = "IF COL_LENGTH(N'EventSearchDocuments', N'BizLocationGLN') IS NULL ALTER TABLE EventSearchDocuments ADD BizLocationGLN nvarchar(450) NULL;";

        /// <summary>
        /// Idempotently creates the index on the BizLocationGLN column; must run in a later batch than <see cref="AddBizLocationColumnSql"/>.
        /// </summary>
        private const string CreateBizLocationIndexSql = $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{ApplicationDbContext.BizLocationGLNIndexName}' AND object_id = OBJECT_ID(N'EventSearchDocuments')) CREATE INDEX {ApplicationDbContext.BizLocationGLNIndexName} ON EventSearchDocuments (BizLocationGLN);";

        public SqlServerService(ILogger<SqlServerService> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task InitializeDatabase()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Check if the database exists
                if (await context.Database.EnsureCreatedAsync())
                {
                    _logger.LogInformation("Database created successfully.");
                }
                else
                {
                    _logger.LogInformation("Database already exists.");
                }

                await EnsureBizLocationColumnAsync(context);
                await BackfillBizLocationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        /// <summary>
        /// Adds the <see cref="EventSearchSqlDocument.BizLocationGLN"/> column and its index to databases created before they existed.
        /// </summary>
        /// <remarks>
        /// The database is created with <c>EnsureCreated</c> rather than EF migrations, and <c>EnsureCreated</c> never alters an
        /// existing schema, so the upgrade is applied with idempotent DDL. New databases already have both from the model,
        /// making this a no-op. The index is created in a separate batch because SQL Server resolves column names per batch.
        /// </remarks>
        /// <param name="context">An open context on the GDST cache database.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the schema cannot be upgraded, typically because the database login lacks ALTER permission on the table.
        /// The message contains the SQL a database administrator can run to apply the upgrade manually.
        /// </exception>
        private async Task EnsureBizLocationColumnAsync(ApplicationDbContext context)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(AddBizLocationColumnSql);
                await context.Database.ExecuteSqlRawAsync(CreateBizLocationIndexSql);
            }
            catch (Exception ex)
            {
                // Note - Claude - 11/10/2026: Failing startup is deliberate. Without the column every search-row insert fails, and
                // keeping the service up would need a second EF model; the login created this schema via EnsureCreated, so this only
                // happens when its permissions were reduced afterwards.
                string manualSql = $"{AddBizLocationColumnSql}\nGO\n{CreateBizLocationIndexSql}\nGO";
                _logger.LogError(ex, "[CRITICAL ERROR]: Could not add the BizLocationGLN column to the EventSearchDocuments table, which this version of the driver requires. Grant the login ALTER permission on the table, or have a database administrator run the following SQL, then restart the driver:\n{ManualSql}", manualSql);
                throw new InvalidOperationException($"The SQL Server schema upgrade for the BizLocationGLN column failed. Run the following SQL as a database administrator, then restart the driver:\n{manualSql}", ex);
            }

            _logger.LogDebug("Ensured the BizLocationGLN search column and index exist.");
        }

        /// <summary>
        /// Sets <see cref="EventSearchSqlDocument.BizLocationGLN"/> on search rows written before that column existed.
        /// </summary>
        /// <remarks>
        /// Earlier versions stored the business location only in <see cref="EventSearchSqlDocument.LocationGLN"/>, mixed with
        /// source and destination locations, so the value is recovered by parsing each event's stored JSON. Events are
        /// processed in batches, and within a batch events sharing a business location are updated in one statement.
        /// Every processed row receives a non-null value (empty when the event has no business location, its JSON cannot be
        /// parsed, or the event row is missing), which guarantees the loop terminates and makes later startups a single
        /// indexed lookup. No explicit transaction is used: each update is idempotent and any rows left unprocessed by an
        /// interruption are picked up on the next startup.
        /// </remarks>
        private async Task BackfillBizLocationAsync()
        {
            const int batchSize = 500;

            _logger.LogDebug("Checking for event search rows that need the bizLocation backfill.");

            int updatedEventCount = 0;
            List<string> failedEventIds = new List<string>();
            Exception? firstFailure = null;
            while (true)
            {
                using ApplicationDbContext context = await _contextFactory.CreateDbContextAsync();

                List<string> eventIds = await context.EventSearchDocuments.Where(x => x.BizLocationGLN == null).Select(x => x.EventId).Distinct().Take(batchSize).ToListAsync();
                if (eventIds.Count == 0)
                {
                    break;
                }

                Dictionary<string, string> eventJsonById = await context.EPCISEvents.Where(e => eventIds.Contains(e.EventId)).ToDictionaryAsync(e => e.EventId, e => e.EventJson);

                // Resolve each event's business location, then group so one UPDATE covers every event at the same location.
                Dictionary<string, List<string>> eventIdsByBizLocation = new Dictionary<string, List<string>>();
                foreach (string eventId in eventIds)
                {
                    string bizLocationGLN = string.Empty;
                    if (eventJsonById.TryGetValue(eventId, out string? eventJson))
                    {
                        try
                        {
                            bizLocationGLN = EventSearchFields.ExtractBizLocationGLN(eventJson) ?? string.Empty;
                        }
                        catch (Exception ex)
                        {
                            firstFailure ??= ex;
                            failedEventIds.Add(eventId);
                        }
                    }

                    if (!eventIdsByBizLocation.TryGetValue(bizLocationGLN, out List<string>? idsAtLocation))
                    {
                        idsAtLocation = new List<string>();
                        eventIdsByBizLocation[bizLocationGLN] = idsAtLocation;
                    }
                    idsAtLocation.Add(eventId);
                }

                foreach (KeyValuePair<string, List<string>> group in eventIdsByBizLocation)
                {
                    List<string> idsAtLocation = group.Value;
                    string bizLocationGLN = group.Key;
                    await context.EventSearchDocuments.Where(x => idsAtLocation.Contains(x.EventId)).ExecuteUpdateAsync(s => s.SetProperty(x => x.BizLocationGLN, bizLocationGLN));
                }

                updatedEventCount += eventIds.Count;
            }

            if (updatedEventCount > 0)
            {
                _logger.LogInformation("Backfilled bizLocation for the search rows of {UpdatedCount} legacy event(s).", updatedEventCount);
            }

            if (failedEventIds.Count > 0)
            {
                _logger.LogWarning(firstFailure, "Could not parse the stored JSON of {FailedCount} event(s) during the bizLocation backfill, so they will not match EQ_bizLocation queries. First event IDs: {EventIds}.", failedEventIds.Count, failedEventIds.Take(10));
            }
        }

        public async Task StoreEventsAsync(List<IEvent> events)
        {
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
                        }
                        else
                        {
                            context.EPCISEvents.Add(doc);
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
        }

        public async Task StoreMasterDataAsync(List<IVocabularyElement> masterData)
        {
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
                }
                else
                {
                    // Preserve the _id field from the existing document
                    masterDataDoc.ID = existingMasterData.ID;

                    // Replace existing master data
                    context.Entry(existingMasterData).CurrentValues.SetValues(masterDataDoc);
                }
            }

            await context.SaveChangesAsync();
        }

        public async Task StoreSyncHistory(SyncHistoryItem syncHistory)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.SyncHistory.AddAsync(syncHistory);
            await context.SaveChangesAsync();
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

            // Match the business location only; source and destination locations must not satisfy EQ_bizLocation.
            if (options.query.EQ_bizLocation.Count > 0)
            {
                List<string> bizLocations = options.query.EQ_bizLocation.Select(loc => loc.ToString().ToLower()).ToList();
                searchQuery = searchQuery.Where(e => bizLocations.Contains(e.BizLocationGLN!));
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
                await context.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing database");
                throw;
            }
        }
    }
}
