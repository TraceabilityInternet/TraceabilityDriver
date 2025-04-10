using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using System.Collections.Concurrent;
using TraceabilityDriver.Models;
using TraceabilityDriver.Models.MongoDB;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Service for storing and querying EPCIS events and master data in a MongoDB database.
    /// </summary>
    public class MongoDBService : IDatabaseService
    {
        private readonly IMongoCollection<EPCISEventDocument> _eventsCollection;
        private readonly IMongoCollection<MasterDataDocument> _masterDataCollection;
        private readonly IMongoCollection<SyncHistoryItem> _syncHistoryCollection;
        private readonly IMongoCollection<LogModel> _logCollection;
        private readonly IEPCISQueryDocumentMapper _jsonMapper;
        private readonly IEPCISQueryDocumentMapper _xmlMapper;

        public MongoDBService(IConfiguration configuration)
        {
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

            _jsonMapper = OpenTraceabilityMappers.EPCISQueryDocument.JSON;
            _xmlMapper = OpenTraceabilityMappers.EPCISQueryDocument.XML;

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

        /// <summary>
        /// Stores a list of events asynchronously in a database, either inserting new records or updating existing
        /// ones.
        /// </summary>
        /// <param name="events">A collection of event objects to be serialized and stored in the database.</param>
        /// <returns>A task representing the asynchronous operation of storing the events.</returns>
        public async Task StoreEventsAsync(List<IEvent> events)
        {
            // Store events
            foreach (var evt in events)
            {
                EPCISQueryDocument doc = new EPCISQueryDocument();
                doc.EPCISVersion = EPCISVersion.V2;
                doc.Header = OpenTraceability.Models.Common.StandardBusinessDocumentHeader.DummyHeader;
                doc.Events.Add(evt);
                string json = OpenTraceability.Mappers.OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(doc);

                var eventDoc = new EPCISEventDocument
                {
                    EventId = evt.EventID.ToString(),
                    EventJson = json,
                    BizStep = evt.BusinessStep.ToString().ToLower(),
                    Action = evt.Action.ToString()?.ToLower() ?? "",
                    EventTime = evt.EventTime,
                    EPCs = evt.Products.Select(p => p.EPC.ToString().ToLower()).ToList(),
                    ProductGTINs = evt.Products.Select(p => p.EPC.GTIN?.ToString().ToLower()).Where(g => g != null).Select(g => g!).ToList(),
                    LocationGLNs = evt.Location?.GLN != null ? new List<string> { evt.Location.GLN.ToString().ToLower() } : new List<string>(),
                    PartyPGLNs = new List<string>()
                };

                // Add trading party PGLNs if it's a GDST event
                if (evt is IGDSTEvent gdstEvent)
                {
                    if (gdstEvent.ProductOwner != null)
                    {
                        eventDoc.PartyPGLNs.Add(gdstEvent.ProductOwner.ToString());
                    }
                    if (gdstEvent.InformationProvider != null)
                    {
                        eventDoc.PartyPGLNs.Add(gdstEvent.InformationProvider.ToString());
                    }
                }

                // Add source and destination PGLNs/GLNs
                foreach (var source in evt.SourceList)
                {
                    if (!string.IsNullOrWhiteSpace(source.Value))
                    {
                        if (source.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Possessor ||
                            source.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Owner)
                        {
                            eventDoc.PartyPGLNs.Add(source.Value);
                        }
                        else if (source.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Location)
                        {
                            eventDoc.LocationGLNs.Add(source.Value);
                        }
                    }
                }

                foreach (var dest in evt.DestinationList)
                {
                    if (!string.IsNullOrWhiteSpace(dest.Value))
                    {
                        if (dest.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Possessor ||
                            dest.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Owner)
                        {
                            eventDoc.PartyPGLNs.Add(dest.Value);
                        }
                        else if (dest.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Location)
                        {
                            eventDoc.LocationGLNs.Add(dest.Value);
                        }
                    }
                }

                // Check if event with this ID already exists
                var filterBuilder = Builders<EPCISEventDocument>.Filter;
                var filter = filterBuilder.Eq(e => e.EventId, evt.EventID.ToString());
                var existingEvent = await _eventsCollection.Find(filter).FirstOrDefaultAsync();

                if (existingEvent == null)
                {
                    // Insert new event
                    await _eventsCollection.InsertOneAsync(eventDoc);
                }
                else
                {
                    // Preserve the _id field from the existing document
                    eventDoc.Id = existingEvent.Id;

                    // Replace existing event
                    await _eventsCollection.ReplaceOneAsync(filter, eventDoc);
                }
            }
        }

        /// <summary>
        /// Stores a list of vocabulary elements in a database, either inserting new entries or updating existing ones.
        /// </summary>
        /// <param name="masterData">A collection of vocabulary elements to be stored or updated in the database.</param>
        /// <returns>This method does not return a value.</returns>
        public async Task StoreMasterDataAsync(List<IVocabularyElement> masterData)
        {
            // Store master data
            foreach (var element in masterData)
            {
                var masterDataDoc = new MasterDataDocument
                {
                    ElementId = element.ID,
                    ElementType = element.GetType().AssemblyQualifiedName ?? "",
                    ElementJson = OpenTraceability.Mappers.OpenTraceabilityMappers.MasterData.GS1WebVocab.Map(element)
                };

                // Check if master data with this ID already exists
                var filterBuilder = Builders<MasterDataDocument>.Filter;
                var filter = filterBuilder.Eq(m => m.ElementId, element.ID);
                var existingMasterData = await _masterDataCollection.Find(filter).FirstOrDefaultAsync();

                if (existingMasterData == null)
                {
                    // Insert new master data
                    await _masterDataCollection.InsertOneAsync(masterDataDoc);
                }
                else
                {
                    // Preserve the _id field from the existing document
                    masterDataDoc.Id = existingMasterData.Id;

                    // Replace existing master data
                    await _masterDataCollection.ReplaceOneAsync(filter, masterDataDoc);
                }
            }
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

        /// <summary>
        /// Queries events based on various filters such as EPC, event time, record time, business step, action, and
        /// location.
        /// </summary>
        /// <param name="options">Contains the criteria for filtering events during the query process.</param>
        /// <returns>An EPCISQueryDocument containing the list of events that match the specified filters.</returns>
        public async Task<EPCISQueryDocument> QueryEvents(EPCISQueryParameters options)
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

            // Execute query
            var events = await _eventsCollection.Find(filter).ToListAsync();

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
        /// Queries master data based on a unique identifier and returns the corresponding vocabulary element.
        /// </summary>
        /// <param name="identifier">The unique identifier used to locate the specific master data entry.</param>
        /// <returns>An instance of IVocabularyElement representing the deserialized master data.</returns>
        /// <exception cref="Exception">Thrown when the master data cannot be found or fails to deserialize.</exception>
        public async Task<IVocabularyElement?> QueryMasterData(string identifier)
        {
            var filterBuilder = Builders<MasterDataDocument>.Filter;
            var filter = filterBuilder.Eq(m => m.ElementId, identifier);
            var masterDataDoc = await _masterDataCollection.Find(filter).FirstOrDefaultAsync();
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
        }

        /// <summary>
        /// Creates indexes for the events and master data collections to optimize query performance and ensure
        /// uniqueness.
        /// </summary>
        /// <returns>This method does not return a value.</returns>
        private async Task CreateIndexes()
        {
            // Create indexes for events collection
            var eventIndexes = new List<CreateIndexModel<EPCISEventDocument>>
            {
                new CreateIndexModel<EPCISEventDocument>(
                    Builders<EPCISEventDocument>.IndexKeys.Ascending(e => e.EventId),
                    new CreateIndexOptions { Unique = true }),
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

            // Create index for master data collection
            await _masterDataCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<MasterDataDocument>(
                    Builders<MasterDataDocument>.IndexKeys.Ascending(m => m.ElementId),
                    new CreateIndexOptions { Unique = true }));

            // Add end time index to the sync history collection
            await _syncHistoryCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<SyncHistoryItem>(
                    Builders<SyncHistoryItem>.IndexKeys.Ascending(s => s.EndTime)));
        }
    }
} 