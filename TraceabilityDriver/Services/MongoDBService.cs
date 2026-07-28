using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Newtonsoft.Json;
using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using OpenTraceability.Utility;
using System.Collections.Concurrent;
using TraceabilityDriver.Models.DB;
using TraceabilityDriver.Models.DB.MongoDB;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Mongo implementation of the IDatabaseService for storing transformed traceability data in a Mongo GDST data cache.
    /// </summary>
    public class MongoDBService : IDatabaseService
    {
        private readonly ILogger<MongoDBService> _logger;
        private readonly IMongoCollection<EPCISEventDocument> _eventsCollection;
        private readonly IMongoCollection<MasterDataDocument> _masterDataCollection;
        private readonly IMongoCollection<SyncHistoryItem> _syncHistoryCollection;
        private readonly IMongoCollection<LogModel> _logCollection;
        private readonly IMongoCollection<TracebackRecord> _tracebacksCollection;
        private readonly IMongoCollection<TracebackItem> _tracebackItemsCollection;
        private readonly IEPCISQueryDocumentMapper _jsonMapper;
        private readonly IEPCISQueryDocumentMapper _xmlMapper;
        private readonly string? _deploymentVersion;

        public MongoDBService(ILogger<MongoDBService> logger, IConfiguration configuration)
        {
            _logger = logger;

            var conventionPack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, t => true);

            var mongoClient = new MongoClient(configuration["MongoDB:ConnectionString"]);
            var database = mongoClient.GetDatabase(configuration["MongoDB:DatabaseName"]);

            _eventsCollection = database.GetCollection<EPCISEventDocument>(configuration["MongoDB:EventsCollectionName"]);
            _masterDataCollection = database.GetCollection<MasterDataDocument>(configuration["MongoDB:MasterDataCollectionName"]);
            _syncHistoryCollection = database.GetCollection<SyncHistoryItem>(configuration["MongoDB:SyncHistoryCollectionName"]);
            _logCollection = database.GetCollection<LogModel>(configuration["MongoDB:LogCollectionName"]);
            _tracebacksCollection = database.GetCollection<TracebackRecord>(configuration["MongoDB:TracebacksCollectionName"] ?? "tracebacks");
            _tracebackItemsCollection = database.GetCollection<TracebackItem>(configuration["MongoDB:TracebackItemsCollectionName"] ?? "tracebackitems");

            _jsonMapper = OpenTraceabilityMappers.EPCISQueryDocument.JSON;
            _xmlMapper = OpenTraceabilityMappers.EPCISQueryDocument.XML;
            _deploymentVersion = configuration["DEPLOYMENT_VERSION"];

            CreateIndexes().Wait();
        }

        /// <summary>
        /// Initializes the database if it hasn't been initialized yet. Checks if the events collection is empty before
        /// proceeding.
        /// </summary>
        /// <returns>Returns a Task that completes when the database initialization check is done.</returns>
        public async Task InitializeDatabase()
        {
            if (await _eventsCollection.CountDocumentsAsync(new BsonDocument()) > 0)
            {
                return; // Database already initialized
            }
        }

        /// <inheritdoc/>
        public async Task<DatabaseStoreResult> StoreEventsAsync(List<IEvent> events, string deploymentVersion, IReadOnlyDictionary<string, CommonEvent> commonEventsByKey)
        {
            DatabaseStoreResult result = new DatabaseStoreResult();

            // Store events
            foreach (var evt in events)
            {
                // The incoming EventID carries the event key (set by the converter). Capture it, then
                // replace the EventID with the real CBV 2.0 content hash before the event is serialized.
                string eventKey = evt.EventID.ToString();
                evt.EventID = new Uri(EventHashGenerator.GenerateHash(evt));

                var eventDoc = new EPCISEventDocument(evt);
                eventDoc.EventKey = eventKey;
                eventDoc.DeploymentVersion = deploymentVersion;
                eventDoc.CommonEventJson = SerializeCommonEvent(eventKey, commonEventsByKey);

                // Synced events are upserted by (event key, deployment version) because the event id is a
                // content hash that changes while the event is still accumulating source rows.
                var filterBuilder = Builders<EPCISEventDocument>.Filter;
                var filter = filterBuilder.Eq(e => e.EventKey, eventKey) & filterBuilder.Eq(e => e.DeploymentVersion, deploymentVersion);
                var existingEvent = await _eventsCollection.Find(filter).FirstOrDefaultAsync();

                if (existingEvent == null)
                {
                    // Insert new event
                    await _eventsCollection.InsertOneAsync(eventDoc);
                    result.CreatedIds.Add(eventDoc.EventId);
                }
                else
                {
                    // Preserve the _id field from the existing document
                    eventDoc.Id = existingEvent.Id;

                    // Replace existing event
                    await _eventsCollection.ReplaceOneAsync(filter, eventDoc);
                    result.UpdatedIds.Add(eventDoc.EventId);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<DatabaseStoreResult> StoreTracebackEventsAsync(List<IEvent> events)
        {
            DatabaseStoreResult result = new DatabaseStoreResult();
            int skippedCount = 0;

            // Tracebacked events keep the event id they arrived with; one is only generated when missing.
            // The same event appearing twice in one store is only inserted once.
            foreach (var evt in events)
            {
                if (evt.EventID == null)
                {
                    evt.EventID = new Uri(EventHashGenerator.GenerateHash(evt));
                }
            }

            foreach (var evt in events.GroupBy(e => e.EventID.ToString()).Select(g => g.First()))
            {
                // Traceback data is never updated: an event is skipped when its id already exists under
                // the current deployment version or as previously tracebacked data (null version). Events
                // stored only under old deployment versions do not block the save.
                var filterBuilder = Builders<EPCISEventDocument>.Filter;
                var existingFilter = filterBuilder.Eq(e => e.EventId, evt.EventID.ToString()) & BuildCurrentOrTracebackVersionFilter();
                if (await _eventsCollection.Find(existingFilter).AnyAsync())
                {
                    skippedCount++;
                    continue;
                }

                var eventDoc = new EPCISEventDocument(evt);
                eventDoc.EventKey = null;
                eventDoc.DeploymentVersion = null;

                await _eventsCollection.InsertOneAsync(eventDoc);
                result.CreatedIds.Add(eventDoc.EventId);
            }

            if (skippedCount > 0)
            {
                _logger.LogInformation("Skipped {SkippedCount} tracebacked event(s) that already exist in the data cache.", skippedCount);
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, CommonEvent>> GetCommonEventsAsync(List<string> eventKeys, string deploymentVersion)
        {
            var filterBuilder = Builders<EPCISEventDocument>.Filter;
            var filter = filterBuilder.In(e => e.EventKey, eventKeys) & filterBuilder.Eq(e => e.DeploymentVersion, deploymentVersion);
            var documents = await _eventsCollection.Find(filter).ToListAsync();

            Dictionary<string, CommonEvent> results = new Dictionary<string, CommonEvent>();
            foreach (var document in documents)
            {
                if (document.EventKey == null || string.IsNullOrWhiteSpace(document.CommonEventJson))
                {
                    continue;
                }

                try
                {
                    CommonEvent? commonEvent = JsonConvert.DeserializeObject<CommonEvent>(document.CommonEventJson);
                    if (commonEvent != null)
                    {
                        results[document.EventKey] = commonEvent;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize the stored common event for event key {EventKey}; the stored event will be replaced instead of merged.", document.EventKey);
                }
            }

            return results;
        }

        /// <summary>
        /// Builds the filter matching documents of the current deployment version or traceback documents
        /// (null version), which is the set of documents that queries serve and traceback stores dedupe against.
        /// </summary>
        private FilterDefinition<EPCISEventDocument> BuildCurrentOrTracebackVersionFilter()
        {
            var filterBuilder = Builders<EPCISEventDocument>.Filter;
            if (string.IsNullOrWhiteSpace(_deploymentVersion))
            {
                return filterBuilder.Eq(e => e.DeploymentVersion, null);
            }

            return filterBuilder.Or(filterBuilder.Eq(e => e.DeploymentVersion, _deploymentVersion), filterBuilder.Eq(e => e.DeploymentVersion, null));
        }

        /// <summary>
        /// Serializes the common event for the given event key, or returns null with a logged warning
        /// when the caller did not provide one.
        /// </summary>
        private string? SerializeCommonEvent(string eventKey, IReadOnlyDictionary<string, CommonEvent> commonEventsByKey)
        {
            if (commonEventsByKey.TryGetValue(eventKey, out CommonEvent? commonEvent))
            {
                return JsonConvert.SerializeObject(commonEvent);
            }

            _logger.LogWarning("No common event was provided for the event key {EventKey}; later sync runs cannot merge additional rows into the stored event.", eventKey);
            return null;
        }

        /// <summary>
        /// Stores a list of synced vocabulary elements, either inserting new entries or updating the
        /// existing entries of the same deployment version.
        /// </summary>
        /// <param name="masterData">A collection of vocabulary elements to be stored or updated in the database.</param>
        /// <param name="deploymentVersion">The deployment version to stamp on the stored elements.</param>
        /// <returns>The element ids that were inserted versus updated.</returns>
        public async Task<DatabaseStoreResult> StoreMasterDataAsync(List<IVocabularyElement> masterData, string deploymentVersion)
        {
            DatabaseStoreResult result = new DatabaseStoreResult();

            // Store master data
            foreach (var element in masterData)
            {
                var masterDataDoc = new MasterDataDocument
                {
                    ElementId = element.ID,
                    DeploymentVersion = deploymentVersion,
                    ElementType = element.GetType().AssemblyQualifiedName ?? "",
                    ElementJson = OpenTraceability.Mappers.OpenTraceabilityMappers.MasterData.GS1WebVocab.Map(element)
                };

                // Check if master data with this ID already exists under the deployment version being stored.
                var filterBuilder = Builders<MasterDataDocument>.Filter;
                var filter = filterBuilder.Eq(m => m.ElementId, element.ID) & filterBuilder.Eq(m => m.DeploymentVersion, deploymentVersion);
                var existingMasterData = await _masterDataCollection.Find(filter).FirstOrDefaultAsync();

                if (existingMasterData == null)
                {
                    // Insert new master data
                    await _masterDataCollection.InsertOneAsync(masterDataDoc);
                    result.CreatedIds.Add(element.ID);
                }
                else
                {
                    // Preserve the _id field from the existing document
                    masterDataDoc.Id = existingMasterData.Id;

                    // Replace existing master data
                    await _masterDataCollection.ReplaceOneAsync(filter, masterDataDoc);
                    result.UpdatedIds.Add(element.ID);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<DatabaseStoreResult> StoreTracebackMasterDataAsync(List<IVocabularyElement> masterData)
        {
            DatabaseStoreResult result = new DatabaseStoreResult();
            int skippedCount = 0;

            var filterBuilder = Builders<MasterDataDocument>.Filter;
            foreach (var element in masterData.GroupBy(x => x.ID).Select(g => g.First()))
            {
                // Traceback master data is never updated: the element is skipped when it already exists
                // under the current deployment version or as previously tracebacked data (null version).
                // Elements stored only under old deployment versions do not block the save.
                FilterDefinition<MasterDataDocument> versionFilter;
                if (string.IsNullOrWhiteSpace(_deploymentVersion))
                {
                    versionFilter = filterBuilder.Eq(m => m.DeploymentVersion, null);
                }
                else
                {
                    versionFilter = filterBuilder.Or(filterBuilder.Eq(m => m.DeploymentVersion, _deploymentVersion), filterBuilder.Eq(m => m.DeploymentVersion, null));
                }

                var existingFilter = filterBuilder.Eq(m => m.ElementId, element.ID) & versionFilter;
                if (await _masterDataCollection.Find(existingFilter).AnyAsync())
                {
                    skippedCount++;
                    continue;
                }

                var masterDataDoc = new MasterDataDocument
                {
                    ElementId = element.ID,
                    DeploymentVersion = null,
                    ElementType = element.GetType().AssemblyQualifiedName ?? "",
                    ElementJson = OpenTraceability.Mappers.OpenTraceabilityMappers.MasterData.GS1WebVocab.Map(element)
                };

                await _masterDataCollection.InsertOneAsync(masterDataDoc);
                result.CreatedIds.Add(element.ID);
            }

            if (skippedCount > 0)
            {
                _logger.LogInformation("Skipped {SkippedCount} tracebacked master data element(s) that already exist in the data cache.", skippedCount);
            }

            return result;
        }

        /// <summary>
        /// Stores synchronization history data for tracking purposes.
        /// </summary>
        /// <param name="syncHistory">Contains the details of the synchronization event to be stored.</param>
        /// <returns>This method does not return a value.</returns>
        public async Task StoreSyncHistory(SyncHistoryItem syncHistory)
        {
            await _syncHistoryCollection.InsertOneAsync(syncHistory);
        }

        /// <inheritdoc/>
        public async Task StoreTracebackAsync(TracebackRecord traceback)
        {
            var filter = Builders<TracebackRecord>.Filter.Eq(t => t.Id, traceback.Id);
            await _tracebacksCollection.ReplaceOneAsync(filter, traceback, new ReplaceOptions { IsUpsert = true });
        }

        /// <inheritdoc/>
        public async Task StoreTracebackItemsAsync(List<TracebackItem> items)
        {
            foreach (var item in items)
            {
                // Upsert by the natural key so retried ledger writes never create duplicate entries.
                var filterBuilder = Builders<TracebackItem>.Filter;
                var filter = filterBuilder.Eq(i => i.TracebackId, item.TracebackId) & filterBuilder.Eq(i => i.ItemType, item.ItemType) & filterBuilder.Eq(i => i.ItemId, item.ItemId);

                var existingItem = await _tracebackItemsCollection.Find(filter).FirstOrDefaultAsync();
                if (existingItem != null)
                {
                    item.Id = existingItem.Id;
                }

                await _tracebackItemsCollection.ReplaceOneAsync(filter, item, new ReplaceOptions { IsUpsert = true });
            }
        }

        /// <inheritdoc/>
        public async Task<List<TracebackRecord>> GetTracebacksAsync(int top = 100, int skip = 0)
        {
            var sort = Builders<TracebackRecord>.Sort.Descending(t => t.StartTime);
            return await _tracebacksCollection.Find(new BsonDocument()).Sort(sort).Skip(skip).Limit(top).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<TracebackRecord?> GetTracebackAsync(string id)
        {
            var filter = Builders<TracebackRecord>.Filter.Eq(t => t.Id, id);
            return await _tracebacksCollection.Find(filter).FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public async Task<List<TracebackItem>> GetTracebackItemsAsync(string tracebackId)
        {
            var filter = Builders<TracebackItem>.Filter.Eq(i => i.TracebackId, tracebackId);
            return await _tracebackItemsCollection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// Queries events based on various filters such as EPC, event time, record time, business step, action, and
        /// location. Searches the synced events of the currently configured deployment version merged with the
        /// tracebacked events, with the synced copy winning when the same event id appears in both.
        /// </summary>
        /// <param name="options">Contains the criteria for filtering events during the query process.</param>
        /// <returns>An EPCISQueryDocument containing the list of events that match the specified filters.</returns>
        public async Task<EPCISQueryDocument> QueryEvents(EPCISQueryParameters options)
        {
            // Serve the synced events of the current deployment version merged with the tracebacked
            // events (null version). Without a configured version only traceback data is served. When the
            // same event id exists both synced and tracebacked, the synced copy wins.
            var queryFilter = BuildEventQueryFilter(options) & BuildCurrentOrTracebackVersionFilter();
            var matchingEvents = await _eventsCollection.Find(queryFilter).ToListAsync();

            List<EPCISEventDocument> events = matchingEvents
                .GroupBy(e => e.EventId)
                .Select(g => g.OrderByDescending(e => e.DeploymentVersion != null).First())
                .ToList();

            // Convert results back to EPCIS events
            var doc = new EPCISQueryDocument();
            doc.EPCISVersion = EPCISVersion.V2;
            doc.Events = new List<IEvent>();

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

        /// <summary>
        /// Builds the event filter for the given EPCIS query parameters.
        /// </summary>
        private static FilterDefinition<EPCISEventDocument> BuildEventQueryFilter(EPCISQueryParameters options)
        {
            var filterBuilder = Builders<EPCISEventDocument>.Filter;
            var filter = filterBuilder.Empty;

            // Apply query filters
            if (options.query.MATCH_anyEPCClass.Count > 0)
            {
                var epcFilters = new List<FilterDefinition<EPCISEventDocument>>();

                foreach (var epc in options.query.MATCH_anyEPCClass)
                {
                    if (epc.EndsWith('*'))
                    {
                        string prefix = epc.Substring(0, epc.IndexOf('*'));
                        epcFilters.Add(filterBuilder.Regex(e => e.EPCs, new BsonRegularExpression($"^{prefix.ToLower()}", "i")));
                    }
                    else
                    {
                        epcFilters.Add(filterBuilder.AnyEq(e => e.EPCs, epc.ToLower()));
                    }
                }

                filter = filter & filterBuilder.Or(epcFilters);
            }

            if (options.query.MATCH_anyEPC.Count > 0)
            {
                var epcFilters = new List<FilterDefinition<EPCISEventDocument>>();

                foreach (var epc in options.query.MATCH_anyEPC)
                {
                    epcFilters.Add(filterBuilder.AnyEq(e => e.EPCs, epc.ToLower()));
                }

                filter = filter & filterBuilder.Or(epcFilters);
            }

            // Add time range filters
            if (options.query.GE_eventTime.HasValue)
            {
                filter = filter & filterBuilder.Gte(e => e.EventTime, options.query.GE_eventTime.Value);
            }

            if (options.query.LE_eventTime.HasValue)
            {
                filter = filter & filterBuilder.Lt(e => e.EventTime, options.query.LE_eventTime.Value);
            }

            // Add record time range filters
            if (options.query.GE_recordTime.HasValue)
            {
                filter = filter & filterBuilder.Gte(e => e.RecordTime, options.query.GE_recordTime.Value);
            }

            if (options.query.LE_recordTime.HasValue)
            {
                filter = filter & filterBuilder.Lt(e => e.RecordTime, options.query.LE_recordTime.Value);
            }

            // Add bizStep filters
            if (options.query.EQ_bizStep?.Count > 0)
            {
                var eventTypeFilters = options.query.EQ_bizStep.Select(et =>
                    filterBuilder.Eq(e => e.BizStep, et.ToString().ToLower())).ToList();
                filter = filter & filterBuilder.Or(eventTypeFilters);
            }

            // Add action filters
            if (options.query.EQ_action?.Count > 0)
            {
                var actionFilters = options.query.EQ_action.Select(a =>
                    filterBuilder.Eq(e => e.Action, a.ToString().ToLower())).ToList();
                filter = filter & filterBuilder.Or(actionFilters);
            }

            // Add location filters
            if (options.query.EQ_bizLocation.Count > 0)
            {
                var locationFilters = options.query.EQ_bizLocation.Select(loc =>
                    filterBuilder.AnyEq(e => e.LocationGLNs, loc.ToString().ToLower())).ToList();
                filter = filter & filterBuilder.Or(locationFilters);
            }

            return filter;
        }

        /// <summary>
        /// Queries master data based on a unique identifier and returns the corresponding vocabulary element.
        /// The element synced under the currently configured deployment version wins; tracebacked master
        /// data is the fallback.
        /// </summary>
        /// <param name="identifier">The unique identifier used to locate the specific master data entry.</param>
        /// <returns>An instance of IVocabularyElement representing the deserialized master data.</returns>
        /// <exception cref="Exception">Thrown when the master data fails to deserialize.</exception>
        public async Task<IVocabularyElement?> QueryMasterData(string identifier)
        {
            var filterBuilder = Builders<MasterDataDocument>.Filter;

            // The synced copy of the current deployment version wins; tracebacked master data (null
            // version) is the fallback. Without a configured version only traceback data is served.
            MasterDataDocument? masterDataDoc = null;
            if (!string.IsNullOrWhiteSpace(_deploymentVersion))
            {
                var syncedFilter = filterBuilder.Eq(m => m.ElementId, identifier) & filterBuilder.Eq(m => m.DeploymentVersion, _deploymentVersion);
                masterDataDoc = await _masterDataCollection.Find(syncedFilter).FirstOrDefaultAsync();
            }

            if (masterDataDoc == null)
            {
                var tracebackFilter = filterBuilder.Eq(m => m.ElementId, identifier) & filterBuilder.Eq(m => m.DeploymentVersion, null);
                masterDataDoc = await _masterDataCollection.Find(tracebackFilter).FirstOrDefaultAsync();
            }

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

        /// <summary>
        /// Gets the latest syncs.
        /// </summary>
        /// <param name="top">Limit the number returned.</param>
        /// <returns>The syncs returned.</returns>
        public async Task<List<SyncHistoryItem>> GetLatestSyncs(int top = 10)
        {
            var sort = Builders<SyncHistoryItem>.Sort.Descending(s => s.EndTime);
            return await _syncHistoryCollection.Find(new BsonDocument()).Sort(sort).Limit(top).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<SyncHistoryItem?> GetLatestSyncAsync(string deploymentVersion)
        {
            var filter = Builders<SyncHistoryItem>.Filter.Eq(s => s.DeploymentVersion, deploymentVersion);
            var sort = Builders<SyncHistoryItem>.Sort.Descending(s => s.EndTime);
            return await _syncHistoryCollection.Find(filter).Sort(sort).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Returns the last errors in the database.
        /// </summary>
        /// <param name="top">The number of errors to return.</param>
        /// <returns>The errors.</returns>
        public async Task<List<LogModel>> GetLastErrors(int top = 10)
        {
            var filter = Builders<LogModel>.Filter.Eq(l => l.Level, "Error");
            var sort = Builders<LogModel>.Sort.Descending(s => s.Timestamp);
            return await _logCollection.Find(filter).Sort(sort).Limit(top).ToListAsync();
        }

        /// <summary>
        /// Generates a comprehensive database report containing statistics about events, master data, and sync operations.
        /// </summary>
        /// <returns>A DatabaseReport object containing counts of various data types in the system.</returns>
        public async Task<DatabaseReport> GetDatabaseReport()
        {
            var report = new DatabaseReport();

            // Get event counts by bizStep
            var eventsBizStepGroups = await _eventsCollection.Aggregate()
                .Group(e => e.BizStep, g => new { BizStep = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var group in eventsBizStepGroups)
            {
                if (!string.IsNullOrEmpty(group.BizStep))
                {
                    report.EventCounts[group.BizStep] = group.Count;
                }
            }

            // Get master data counts by type
            var masterDataTypeGroups = await _masterDataCollection.Aggregate()
                .Group(m => m.ElementType, g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

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
            var syncStatusGroups = await _syncHistoryCollection.Aggregate()
                .Group(s => s.Status, g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var group in syncStatusGroups)
            {
                report.SyncCounts[group.Status] = group.Count;
            }

            return report;
        }

        /// <summary>
        /// Clears all data from the database by dropping all collections.
        /// </summary>
        /// <returns>A task representing the asynchronous operation of clearing the database.</returns>
        public async Task ClearDatabaseAsync()
        {
            // Clear all collections
            await _eventsCollection.DeleteManyAsync(new BsonDocument());
            await _masterDataCollection.DeleteManyAsync(new BsonDocument());
            await _syncHistoryCollection.DeleteManyAsync(new BsonDocument());
            await _logCollection.DeleteManyAsync(new BsonDocument());
            await _tracebacksCollection.DeleteManyAsync(new BsonDocument());
            await _tracebackItemsCollection.DeleteManyAsync(new BsonDocument());
        }

        /// <summary>
        /// Creates indexes for the events and master data collections to optimize query performance and ensure
        /// uniqueness.
        /// </summary>
        /// <remarks>
        /// Runs on every startup, so every index creation must be idempotent. Databases created before
        /// deployment versioning carry unique single-field indexes on the event id and element id, and
        /// databases created while traceback data was stored separately carry a unique
        /// (event id, deployment version) index; both conflict with the current uniqueness rules and are
        /// dropped by name before the new indexes are created.
        /// </remarks>
        /// <returns>This method does not return a value.</returns>
        private async Task CreateIndexes()
        {
            // Drop the superseded unique indexes; without this, storing a traceback copy of a synced
            // event or the same element under a second deployment version would throw a duplicate key error.
            await DropIndexIfExistsAsync(_eventsCollection, "EventId_1");
            await DropIndexIfExistsAsync(_eventsCollection, "EventId_1_DeploymentVersion_1");
            await DropIndexIfExistsAsync(_masterDataCollection, "ElementId_1");

            // Create indexes for events collection. Synced events are unique per (event key, deployment
            // version); the filter exempts traceback documents, which have no event key. The event id is
            // indexed non-unique because the same content hash can legitimately appear on multiple
            // documents (a traceback copy of a synced event, or the same content under two versions).
            var eventIndexes = new List<CreateIndexModel<EPCISEventDocument>>
            {
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.EventKey).Ascending(e => e.DeploymentVersion),
                    new CreateIndexOptions<EPCISEventDocument> { Unique = true, PartialFilterExpression = Builders<EPCISEventDocument>.Filter.Type(e => e.EventKey, BsonType.String) }),
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.EventId)),
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.DeploymentVersion)),
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.EventTime)),
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.EPCs)),
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.ProductGTINs)),
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.LocationGLNs)),
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.PartyPGLNs)),
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.RecordTime)),
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.BizStep)),
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.Action))
            };

            await _eventsCollection.Indexes.CreateManyAsync(eventIndexes);

            // Create index for master data collection. Master data is unique per (element id, deployment
            // version); Mongo treats the null version on traceback documents as a value, so this also
            // enforces one traceback document per element id.
            await _masterDataCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<MasterDataDocument>(
                    Builders<MasterDataDocument>.IndexKeys.Ascending(m => m.ElementId).Ascending(m => m.DeploymentVersion),
                    new CreateIndexOptions { Unique = true }));

            // Add end time index to the sync history collection
            await _syncHistoryCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<SyncHistoryItem>(
                    Builders<SyncHistoryItem>.IndexKeys.Ascending(s => s.EndTime)));

            // Add start time index to the tracebacks collection for the newest-first listing.
            await _tracebacksCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<TracebackRecord>(
                    Builders<TracebackRecord>.IndexKeys.Descending(t => t.StartTime)));

            // The unique compound index is what makes ledger writes idempotent under retries.
            await _tracebackItemsCollection.Indexes.CreateManyAsync(new List<CreateIndexModel<TracebackItem>>
            {
                new CreateIndexModel<TracebackItem>(
                    Builders<TracebackItem>.IndexKeys.Ascending(i => i.TracebackId).Ascending(i => i.ItemType).Ascending(i => i.ItemId),
                    new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<TracebackItem>(
                    Builders<TracebackItem>.IndexKeys.Ascending(i => i.TracebackId))
            });
        }

        /// <summary>
        /// Drops the index with the given name when it exists on the collection. A no-op on fresh
        /// databases where the collection or index does not exist yet.
        /// </summary>
        private static async Task DropIndexIfExistsAsync<T>(IMongoCollection<T> collection, string indexName)
        {
            using var cursor = await collection.Indexes.ListAsync();
            List<BsonDocument> indexes = await cursor.ToListAsync();
            if (indexes.Any(i => i["name"] == indexName))
            {
                await collection.Indexes.DropOneAsync(indexName);
            }
        }
    }
} 