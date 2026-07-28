using Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using OpenTraceability.Utility;
using System.Collections.Concurrent;
using TraceabilityDriver.Extensions;
using TraceabilityDriver.Models.DB;
using TraceabilityDriver.Models.DB.Sql;
using TraceabilityDriver.Models.Mapping;
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
        private readonly string? _deploymentVersion;

        public SqlServerService(ILogger<SqlServerService> logger, IDbContextFactory<ApplicationDbContext> contextFactory, IConfiguration configuration)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _deploymentVersion = configuration["DEPLOYMENT_VERSION"];
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

        /// <inheritdoc/>
        public async Task<DatabaseStoreResult> StoreEventsAsync(List<IEvent> events, string deploymentVersion)
        {
            ConcurrentBag<string> createdIds = new ConcurrentBag<string>();
            ConcurrentBag<string> updatedIds = new ConcurrentBag<string>();

            // The incoming EventID carries the event key (set by the converter). Capture it before the
            // EventID is replaced with the real CBV 2.0 content hash, which can only be generated once the
            // event has been merged with its stored copy.
            List<(IEvent Event, string EventKey)> keyedEvents = new List<(IEvent, string)>();
            foreach (IEvent evt in events)
            {
                keyedEvents.Add((evt, evt.EventID.ToString()));
            }

            List<List<(IEvent Event, string EventKey)>> batches = keyedEvents.Batch(42); // 42 is a magic number for performance when adding entities using ef core.
            await Parallel.ForEachAsync(batches, async (batch, ct) =>
            {
                using (var context = await _contextFactory.CreateDbContextAsync())
                {
                    // rather than check for events to update in the loop below, we want to execute one
                    // large query to get all the events that will be updated rather than added. Synced
                    // events are upserted by (event key, deployment version) because the event id is a
                    // content hash that changes while the event is still accumulating source rows.
                    List<string> storingEventKeys = batch.Select(x => x.EventKey).ToList();
                    var existingEvents = await context.EPCISEvents.Where(x => x.EventKey != null && storingEventKeys.Contains(x.EventKey) && x.DeploymentVersion == deploymentVersion).ToDictionaryAsync(x => x.EventKey!, y => y);

                    List<EventSearchSqlDocument> searchDocuments = new List<EventSearchSqlDocument>();
                    foreach ((IEvent incomingEvent, string eventKey) in batch)
                    {
                        existingEvents.TryGetValue(eventKey, out EPCISEventSqlDocument? existingEvent);

                        // The rows of one event can straddle sync runs, so the stored copy is enriched with
                        // the incoming one rather than replaced by it. The stored copy is the merge target
                        // because its values came from earlier rows, which take priority under the first-wins
                        // merge convention.
                        IEvent evt = incomingEvent;
                        if (existingEvent != null)
                        {
                            IEvent? storedEvent = DeserializeStoredEvent(existingEvent.EventJson, eventKey);
                            if (storedEvent != null)
                            {
                                storedEvent.Merge(incomingEvent);
                                evt = storedEvent;
                            }
                        }

                        // The event id must be generated from the merged content, so it is only assigned once
                        // the merge is done.
                        evt.EventID = new Uri(EventHashGenerator.GenerateHash(evt));

                        EPCISEventSqlDocument doc = new EPCISEventSqlDocument(evt);
                        doc.EventKey = eventKey;
                        doc.DeploymentVersion = deploymentVersion;

                        if (existingEvent != null)
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

                        // Build the search rows per event so each row carries the event key it belongs to.
                        List<EventSearchSqlDocument> eventSearchRows = EventSearchSqlDocument.CreateSearchDocuments(new List<IEvent> { evt });
                        eventSearchRows.ForEach(x => { x.EventKey = eventKey; x.DeploymentVersion = deploymentVersion; });
                        searchDocuments.AddRange(eventSearchRows);

                        // The caller keeps a reference to the incoming event, so it must end up carrying the
                        // id the merged event was stored under.
                        incomingEvent.EventID = evt.EventID;
                    }

                    // Batch save the search documents by first deleting all existing index documents for
                    // the given event keys and then adding the new ones. The delete is scoped by event key
                    // and deployment version so stale rows pointing at a superseded event id are removed
                    // while rows from other versions and traceback rows persist.
                    var existingSearchDocuments = await context.EventSearchDocuments
                    .Where(x => x.EventKey != null && storingEventKeys.Contains(x.EventKey) && x.DeploymentVersion == deploymentVersion)
                            .ToListAsync();

                    context.EventSearchDocuments.RemoveRange(existingSearchDocuments);
                    context.EventSearchDocuments.AddRange(searchDocuments);

                    await context.SaveChangesAsync();
                }
            });

            return new DatabaseStoreResult { CreatedIds = createdIds.ToList(), UpdatedIds = updatedIds.ToList() };
        }

        /// <inheritdoc/>
        public async Task<DatabaseStoreResult> StoreTracebackEventsAsync(List<IEvent> events)
        {
            ConcurrentBag<string> createdIds = new ConcurrentBag<string>();
            int skippedCount = 0;

            // Tracebacked events keep the event id they arrived with; one is only generated when missing.
            foreach (IEvent evt in events)
            {
                if (evt.EventID == null)
                {
                    evt.EventID = new Uri(EventHashGenerator.GenerateHash(evt));
                }
            }

            // Deduplicate by event id so the same event appearing twice in one store is only inserted once.
            List<IEvent> distinctEvents = events.GroupBy(e => e.EventID.ToString()).Select(g => g.First()).ToList();

            List<List<IEvent>> batches = distinctEvents.Batch(42); // 42 is a magic number for performance when adding entities using ef core.
            await Parallel.ForEachAsync(batches, async (batch, ct) =>
            {
                using (var context = await _contextFactory.CreateDbContextAsync())
                {
                    // Traceback data is never updated: an event is skipped when its id already exists under
                    // the current deployment version or as previously tracebacked data (null version).
                    // Events stored only under old deployment versions do not block the save.
                    List<string> storingEventIds = batch.Select(x => x.EventID.ToString()).ToList();
                    IQueryable<EPCISEventSqlDocument> existingQuery = context.EPCISEvents.Where(x => storingEventIds.Contains(x.EventId));
                    if (string.IsNullOrWhiteSpace(_deploymentVersion))
                    {
                        existingQuery = existingQuery.Where(x => x.DeploymentVersion == null);
                    }
                    else
                    {
                        existingQuery = existingQuery.Where(x => x.DeploymentVersion == _deploymentVersion || x.DeploymentVersion == null);
                    }
                    List<string> existingEventIds = await existingQuery.Select(x => x.EventId).Distinct().ToListAsync();

                    List<IEvent> newEvents = batch.Where(x => !existingEventIds.Contains(x.EventID.ToString())).ToList();
                    Interlocked.Add(ref skippedCount, batch.Count - newEvents.Count);

                    foreach (IEvent evt in newEvents)
                    {
                        EPCISEventSqlDocument doc = new EPCISEventSqlDocument(evt);
                        context.EPCISEvents.Add(doc);
                        createdIds.Add(evt.EventID.ToString());
                    }

                    // Search rows are only inserted for the newly created events; skipped events keep the
                    // rows of the copy that blocked them.
                    List<EventSearchSqlDocument> searchDocuments = EventSearchSqlDocument.CreateSearchDocuments(newEvents);
                    context.EventSearchDocuments.AddRange(searchDocuments);

                    await context.SaveChangesAsync();
                }
            });

            if (skippedCount > 0)
            {
                _logger.LogInformation("Skipped {SkippedCount} tracebacked event(s) that already exist in the data cache.", skippedCount);
            }

            return new DatabaseStoreResult { CreatedIds = createdIds.ToList() };
        }

        /// <inheritdoc/>
        public async Task<DatabaseStoreResult> StoreMasterDataAsync(List<IVocabularyElement> masterData, string deploymentVersion)
        {
            DatabaseStoreResult result = new DatabaseStoreResult();

            using var context = await _contextFactory.CreateDbContextAsync();
            foreach (var incomingElement in masterData)
            {
                // Check if master data with this ID already exists under the deployment version being stored.
                var existingMasterData = await context.MasterDataDocuments.FirstOrDefaultAsync(x => x.ElementId == incomingElement.ID && x.DeploymentVersion == deploymentVersion);

                // The rows describing one element can straddle sync runs, so the stored copy is enriched with
                // the incoming one rather than replaced by it. The stored copy is the merge target because its
                // values came from earlier rows, which take priority under the first-wins merge convention.
                IVocabularyElement element = incomingElement;
                if (existingMasterData != null)
                {
                    IVocabularyElement? storedElement = DeserializeStoredMasterData(existingMasterData.ElementType, existingMasterData.ElementJson, existingMasterData.ElementId);
                    if (storedElement != null)
                    {
                        storedElement.Merge(incomingElement);
                        element = storedElement;
                    }
                }

                var masterDataDoc = new MasterDataSqlDocument
                {
                    ElementId = element.ID,
                    DeploymentVersion = deploymentVersion,
                    ElementType = element.GetType().AssemblyQualifiedName ?? "",
                    ElementJson = OpenTraceability.Mappers.OpenTraceabilityMappers.MasterData.GS1WebVocab.Map(element)
                };

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

        /// <summary>
        /// Deserializes a stored event back into an <see cref="IEvent"/> so an incoming partial copy can be
        /// merged into it, or returns null with a logged warning when it cannot be read.
        /// </summary>
        /// <remarks>
        /// A stored event that cannot be deserialized is replaced rather than merged. That loses the earlier
        /// rows' data, but it is the only way forward and a hard failure would stall every later sync run.
        /// </remarks>
        private IEvent? DeserializeStoredEvent(string eventJson, string eventKey)
        {
            if (string.IsNullOrWhiteSpace(eventJson))
            {
                return null;
            }

            try
            {
                EPCISQueryDocument storedDocument = OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(eventJson);
                IEvent? storedEvent = storedDocument.Events.FirstOrDefault();
                if (storedEvent == null)
                {
                    _logger.LogWarning("The stored event for event key {EventKey} held no event; the stored event will be replaced instead of merged.", eventKey);
                }

                return storedEvent;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the stored event for event key {EventKey}; the stored event will be replaced instead of merged.", eventKey);
                return null;
            }
        }

        /// <summary>
        /// Deserializes a stored master data element so an incoming partial copy can be merged into it, or
        /// returns null with a logged warning when it cannot be read.
        /// </summary>
        /// <remarks>
        /// A stored element that cannot be deserialized is replaced rather than merged. That loses the earlier
        /// rows' data, but it is the only way forward and a hard failure would stall every later sync run.
        /// </remarks>
        private IVocabularyElement? DeserializeStoredMasterData(string elementType, string elementJson, string elementId)
        {
            if (string.IsNullOrWhiteSpace(elementJson) || string.IsNullOrWhiteSpace(elementType))
            {
                return null;
            }

            try
            {
                Type? resolvedType = Type.GetType(elementType);
                if (resolvedType == null)
                {
                    _logger.LogWarning("Failed to resolve the stored element type {ElementType} for element {ElementId}; the stored element will be replaced instead of merged.", elementType, elementId);
                    return null;
                }

                return OpenTraceabilityMappers.MasterData.GS1WebVocab.Map(resolvedType, elementJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the stored master data for element {ElementId}; the stored element will be replaced instead of merged.", elementId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<DatabaseStoreResult> StoreTracebackMasterDataAsync(List<IVocabularyElement> masterData)
        {
            DatabaseStoreResult result = new DatabaseStoreResult();
            int skippedCount = 0;

            using var context = await _contextFactory.CreateDbContextAsync();
            foreach (var element in masterData.GroupBy(x => x.ID).Select(g => g.First()))
            {
                // Traceback master data is never updated: the element is skipped when it already exists
                // under the current deployment version or as previously tracebacked data (null version).
                // Elements stored only under old deployment versions do not block the save.
                IQueryable<MasterDataSqlDocument> existingQuery = context.MasterDataDocuments.Where(x => x.ElementId == element.ID);
                if (string.IsNullOrWhiteSpace(_deploymentVersion))
                {
                    existingQuery = existingQuery.Where(x => x.DeploymentVersion == null);
                }
                else
                {
                    existingQuery = existingQuery.Where(x => x.DeploymentVersion == _deploymentVersion || x.DeploymentVersion == null);
                }

                if (await existingQuery.AnyAsync())
                {
                    skippedCount++;
                    continue;
                }

                var masterDataDoc = new MasterDataSqlDocument
                {
                    ElementId = element.ID,
                    DeploymentVersion = null,
                    ElementType = element.GetType().AssemblyQualifiedName ?? "",
                    ElementJson = OpenTraceability.Mappers.OpenTraceabilityMappers.MasterData.GS1WebVocab.Map(element)
                };

                await context.AddAsync(masterDataDoc);
                result.CreatedIds.Add(element.ID);
            }

            await context.SaveChangesAsync();

            if (skippedCount > 0)
            {
                _logger.LogInformation("Skipped {SkippedCount} tracebacked master data element(s) that already exist in the data cache.", skippedCount);
            }

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

            // Serve the synced events of the current deployment version merged with the tracebacked
            // events (null version). Without a configured version only traceback data is served.
            IQueryable<EventSearchSqlDocument> searchQuery;
            IQueryable<EPCISEventSqlDocument> eventsQuery;
            if (string.IsNullOrWhiteSpace(_deploymentVersion))
            {
                searchQuery = context.EventSearchDocuments.Where(e => e.DeploymentVersion == null);
                eventsQuery = context.EPCISEvents.Where(e => e.DeploymentVersion == null);
            }
            else
            {
                searchQuery = context.EventSearchDocuments.Where(e => e.DeploymentVersion == _deploymentVersion || e.DeploymentVersion == null);
                eventsQuery = context.EPCISEvents.Where(e => e.DeploymentVersion == _deploymentVersion || e.DeploymentVersion == null);
            }

            searchQuery = ApplyEventFilters(searchQuery, options);

            // Get unique event IDs from the search results
            var matchingEventIds = await searchQuery
                .Select(e => e.EventId)
                .Distinct()
                .ToListAsync();

            // Now query the actual EPCIS events using the event IDs from search. When the same event id
            // exists both synced and tracebacked, the synced copy wins.
            var matchingEvents = await eventsQuery
                .Where(e => matchingEventIds.Contains(e.EventId))
                .Select(e => new { e.EventId, e.DeploymentVersion, e.EventJson })
                .ToListAsync();

            List<string> eventJsonList = matchingEvents
                .GroupBy(e => e.EventId)
                .Select(g => g.OrderByDescending(e => e.DeploymentVersion != null).First().EventJson)
                .ToList();

            // Convert results back to EPCIS events
            var doc = new EPCISQueryDocument
            {
                EPCISVersion = EPCISVersion.V2,
                Events = new List<IEvent>()
            };

            ConcurrentBag<EPCISQueryDocument> queryDocs = new();
            Parallel.ForEach(eventJsonList, (eventJson, ct) =>
            {
                EPCISQueryDocument queryDoc = OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(eventJson);
                queryDocs.Add(queryDoc);
            });

            foreach (var queryDoc in queryDocs)
            {
                doc.Merge(queryDoc);
            }

            return doc;
        }

        /// <summary>
        /// Applies the EPCIS query filters to a search document query.
        /// </summary>
        private static IQueryable<EventSearchSqlDocument> ApplyEventFilters(IQueryable<EventSearchSqlDocument> searchQuery, EPCISQueryParameters options)
        {
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

            return searchQuery;
        }

        public async Task<IVocabularyElement?> QueryMasterData(string identifier)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // The synced copy of the current deployment version wins; tracebacked master data (null
            // version) is the fallback. Without a configured version only traceback data is served.
            IQueryable<MasterDataSqlDocument> query = context.MasterDataDocuments.Where(x => x.ElementId == identifier);
            if (string.IsNullOrWhiteSpace(_deploymentVersion))
            {
                query = query.Where(x => x.DeploymentVersion == null);
            }
            else
            {
                query = query.Where(x => x.DeploymentVersion == _deploymentVersion || x.DeploymentVersion == null);
            }

            var masterDataDoc = await query.OrderBy(x => x.DeploymentVersion == null ? 1 : 0).FirstOrDefaultAsync();
            if (masterDataDoc == null)
            {
                return null;
            }

            Type t = Type.GetType(masterDataDoc.ElementType)
                    ?? throw new Exception($"Failed to get type: {masterDataDoc.ElementType}");
            return OpenTraceabilityMappers.MasterData.GS1WebVocab.Map(t, masterDataDoc.ElementJson);
        }

        public async Task<List<SyncHistoryItem>> GetLatestSyncs(int top = 10)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.SyncHistory.OrderByDescending(x => x.EndTime).Take(top).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<SyncHistoryItem?> GetLatestSyncAsync(string deploymentVersion)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.SyncHistory.Where(x => x.DeploymentVersion == deploymentVersion).OrderByDescending(x => x.EndTime).FirstOrDefaultAsync();
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
