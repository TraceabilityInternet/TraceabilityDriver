using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json;
using OpenTraceability.GDST;
using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Models.MasterData;
using OpenTraceability.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityDriver.Models.MongoDB;

namespace TraceabilityDriver.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<EPCISEventDocument> _eventsCollection;
        private readonly IMongoCollection<MasterDataDocument> _masterDataCollection;
        private readonly IEPCISQueryDocumentMapper _jsonMapper;
        private readonly IEPCISQueryDocumentMapper _xmlMapper;

        public MongoDBService(IConfiguration configuration)
        {
            var mongoClient = new MongoClient(configuration["MongoDB:ConnectionString"]);
            var database = mongoClient.GetDatabase(configuration["MongoDB:DatabaseName"]);
            
            _eventsCollection = database.GetCollection<EPCISEventDocument>(configuration["MongoDB:EventsCollectionName"]);
            _masterDataCollection = database.GetCollection<MasterDataDocument>(configuration["MongoDB:MasterDataCollectionName"]);
            
            _jsonMapper = OpenTraceabilityMappers.EPCISQueryDocument.JSON;
            _xmlMapper = OpenTraceabilityMappers.EPCISQueryDocument.XML;

            CreateIndexes().Wait();
        }

        public async Task InitializeDatabase()
        {
            if (await _eventsCollection.CountDocumentsAsync(new BsonDocument()) > 0)
            {
                return; // Database already initialized
            }
        }

        public async Task StoreEventsAsync(List<IEvent> events)
        {
            // Store events
            foreach (var evt in events)
            {
                var eventJson = Newtonsoft.Json.JsonConvert.SerializeObject(evt, new JsonSerializerSettings() {
                    TypeNameHandling = TypeNameHandling.All
                });
                
                var eventDoc = new EPCISEventDocument
                {
                    EventId = evt.EventID.ToString(),
                    EventJson = eventJson,
                    BizStep = evt.BusinessStep.ToString(),
                    Action = evt.Action.ToString() ?? "",
                    EventTime = evt.EventTime,
                    EPCs = evt.Products.Select(p => p.EPC.ToString()).ToList(),
                    ProductGTINs = evt.Products.Select(p => p.EPC.GTIN?.ToString()).Where(g => g != null).Select(g => g!).ToList(),
                    LocationGLNs = evt.Location?.GLN != null ? new List<string> { evt.Location.GLN.ToString() } : new List<string>(),
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

        public async Task StoreMasterDataAsync(List<IVocabularyElement> masterData)
        {
            // Store master data
            foreach (var element in masterData)
            {
                var elementJson = Newtonsoft.Json.JsonConvert.SerializeObject(element, new JsonSerializerSettings() {
                    TypeNameHandling = TypeNameHandling.All
                });
                
                var masterDataDoc = new MasterDataDocument
                {
                    ElementId = element.ID,
                    ElementType = element.GetType().Name,
                    ElementJson = elementJson
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

        public async Task<EPCISQueryDocument> QueryEvents(EPCISQueryParameters options)
        {
            var filterBuilder = Builders<EPCISEventDocument>.Filter;
            var filter = filterBuilder.Empty;

            // Apply query filters
            if (options.query.MATCH_anyEPC.Count > 0)
            {
                var epcFilters = new List<FilterDefinition<EPCISEventDocument>>();
                
                foreach (var epc in options.query.MATCH_anyEPC)
                {
                    if (epc.EndsWith('*'))
                    {
                        string prefix = epc.Substring(0, epc.IndexOf('*'));
                        epcFilters.Add(filterBuilder.Regex(e => e.EPCs, new BsonRegularExpression($"^{prefix}", "i")));
                    }
                    else
                    {
                        epcFilters.Add(filterBuilder.AnyEq(e => e.EPCs, epc));
                    }
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
                    filterBuilder.Eq(e => e.BizStep, et.ToString())).ToList();
                filter = filter & filterBuilder.Or(eventTypeFilters);
            }

            // Add action filters
            if (options.query.EQ_action?.Count > 0)
            {
                var actionFilters = options.query.EQ_action.Select(a => 
                    filterBuilder.Eq(e => e.Action, a.ToString())).ToList();
                filter = filter & filterBuilder.Or(actionFilters);
            }

            // Add location filters
            if (options.query.EQ_bizLocation.Count > 0)
            {
                var locationFilters = options.query.EQ_bizLocation.Select(loc => 
                    filterBuilder.AnyEq(e => e.LocationGLNs, loc.ToString())).ToList();
                filter = filter & filterBuilder.Or(locationFilters);
            }

            // Execute query
            var eventDocs = await _eventsCollection.Find(filter).ToListAsync();
            
            // Convert results back to EPCIS events
            var result = new EPCISQueryDocument();
            result.Events = new List<IEvent>();
            
            foreach (var doc in eventDocs)
            {
                var evt = Newtonsoft.Json.JsonConvert.DeserializeObject<IEvent>(doc.EventJson, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                result.Events.Add(evt);
            }

            return result;
        }

        public async Task<IVocabularyElement> QueryMasterData(string identifier)
        {
            var filterBuilder = Builders<MasterDataDocument>.Filter;
            var filter = filterBuilder.Eq(m => m.ElementId, identifier);
            var masterDataDoc = await _masterDataCollection.Find(filter).FirstOrDefaultAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<IVocabularyElement>(masterDataDoc.ElementJson, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            }) ?? throw new Exception($"Master data for identifier {identifier} not found or failed to deserialize.");
        }

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
        }
    }
} 