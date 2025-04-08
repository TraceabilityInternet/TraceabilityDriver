using Extensions;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Models;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Models.Sql;

namespace TraceabilityDriver.Services
{
    public class SqlServerService : IDatabaseService
    {
        private readonly ILogger<SqlServerService> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

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

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
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

            foreach (var evt in events)
            {
                EPCISQueryDocument queryDoc = OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(evt.EventJson);
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
            return await context.Logs.OrderByDescending(x => x.Timestamp).Take(top).ToListAsync();
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
