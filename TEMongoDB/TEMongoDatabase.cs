using TraceabilityEngine.Interfaces.DB.DocumentDB;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Databases.Mongo.Serializers;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.Security;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Databases.Mongo
{
    public class TEMongoDatabase : ITEDocumentDB
    {
        private bool _disposedValue;
        private string _connectionString;
        private string _databaseName;
        private MongoClient _client;
        private IMongoDatabase _db;

        static TEMongoDatabase()
        { 
            BsonSerializer.RegisterSerializer(typeof(IPGLN), new PGLNSerializer());
            //BsonSerializer.RegisterSerializer(typeof(TEEventErrorType), new EnumSerializer<TEEventErrorType>());
            //BsonSerializer.RegisterSerializer(typeof(IDID), new DIDSerializer());
        }

        public TEMongoDatabase(string connectionString, string databaseName)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));

            _connectionString = connectionString;
            _databaseName = databaseName;

            _client = new MongoClient(connectionString);
            _db = _client.GetDatabase(_databaseName);
        }

        public async Task<T> LoadAsync<T>(string ObjectID, string collectionName) where T : ITEDocumentObject
        {
            if (string.IsNullOrWhiteSpace(ObjectID))
            {
                throw new ArgumentNullException(nameof(ObjectID));
            }

            if (ObjectId.TryParse(ObjectID, out ObjectId objID))
            {
                var collection = _db.GetCollection<T>(collectionName);
                var filters = Builders<T>.Filter.Eq("ObjectID", ObjectID);
                T item = (await collection.FindAsync<T>(filters)).FirstOrDefault();
                return item;
            }
            else
            {
                throw new FormatException("The ID provided is not in the MongoDB ObjectId format. ObjectID=" + ObjectID);
            }
        }

        public async Task<T> LoadAsync<T>(string field, object value, string collectionName) where T : ITEDocumentObject
        {
            if (string.IsNullOrWhiteSpace(field)) throw new ArgumentNullException(nameof(field));
            if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var collection = _db.GetCollection<T>(collectionName);
            var filters = Builders<T>.Filter.Eq(field, value);
            T item = (await collection.FindAsync<T>(filters)).FirstOrDefault();
            return item;
        }

        public async Task<T> LoadAsync<T>(List<KeyValuePair<string, object>> filters, string collectionName) where T : ITEDocumentObject
        {
            if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));
            if (filters == null) throw new ArgumentNullException(nameof(filters));
            if (filters.Count < 0) throw new ArgumentException("The argument 'filters' requires at least one filter.");

            var collection = _db.GetCollection<T>(collectionName);
            List<FilterDefinition<T>> filterList = new List<FilterDefinition<T>>();
            foreach(var kvp in filters)
            {
                filterList.Add(Builders<T>.Filter.Eq(kvp.Key, kvp.Value));
            }
            var mongoFilters = Builders<T>.Filter.And(filterList);
            T item = (await collection.FindAsync<T>(mongoFilters)).FirstOrDefault();
            return item;
        }

        public async Task<List<T>> LoadManyAsync<T, T2>(string field, List<T2> values, string collectionName) where T : ITEDocumentObject
        {
            if (string.IsNullOrWhiteSpace(field)) throw new ArgumentNullException(nameof(field));
            if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Count < 1) throw new ArgumentException("The argument 'values' has no items. You must supply at least one value to filter on.");

            var collection = _db.GetCollection<T>(collectionName);
            var filters = Builders<T>.Filter.In(field, values);
            List<T> items = await (await collection.FindAsync<T>(filters)).ToListAsync();
            return items;
        }

        public async Task<List<T>> LoadAll<T>(string collectionName) where T : ITEDocumentObject
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));

                var collection = _db.GetCollection<T>(collectionName);
                List<T> items = await (await collection.FindAsync<T>(Builders<T>.Filter.Empty)).ToListAsync();
                return items;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<bool> Exists<T>(string field, object value, string collectionName) where T : ITEDocumentObject
        {
            if (string.IsNullOrWhiteSpace(field)) throw new ArgumentNullException(nameof(field));
            if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var collection = _db.GetCollection<T>(collectionName);
            var filters = Builders<T>.Filter.Eq(field, value);
            if ((await collection.FindAsync<T>(filters)).Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task SaveAsync<T>(T obj, string collectionName) where T : ITEDocumentObject
        {
            if (string.IsNullOrWhiteSpace(obj.ObjectID))
            {
                obj.ObjectID = ObjectId.GenerateNewId().ToString();
            }

            var collection = _db.GetCollection<T>(collectionName);
            var filters = Builders<T>.Filter.Eq("ObjectID", obj.ObjectID);
            ReplaceOptions options = new ReplaceOptions()
            {
                IsUpsert = true
            };
            ReplaceOneResult result = await collection.ReplaceOneAsync(filters, obj, options);
        }

        public async Task SaveAsync<T>(T obj, string field, object value, string collectionName) where T : ITEDocumentObject
        {
            if (string.IsNullOrWhiteSpace(obj.ObjectID))
            {
                obj.ObjectID = ObjectId.GenerateNewId().ToString();
            }

            var collection = _db.GetCollection<T>(collectionName);
            var filters = Builders<T>.Filter.Eq(field, value);
            ReplaceOptions options = new ReplaceOptions()
            {
                IsUpsert = true
            };
            ReplaceOneResult result = await collection.ReplaceOneAsync(filters, obj, options);
        }

        public async Task UpdateField<T>(string matchField, object matchValue, string field, object value, string collectionName) where T : ITEDocumentObject
        {
            var collection = _db.GetCollection<T>(collectionName);
            var filters = Builders<T>.Filter.Eq(matchField, matchValue);
            var update = Builders<T>.Update.Set(field, value);
            var options = new UpdateOptions()
            {
                IsUpsert = false
            };
            UpdateResult result = await collection.UpdateManyAsync(filters, update, options);
        }

        public async Task DropAsync(string collectionName)
        {
            await _db.DropCollectionAsync(collectionName);
        }

        public async Task DeleteOneAsync<T>(string field, object value, string collectionName) where T : ITEDocumentObject
        {
            var collection = _db.GetCollection<T>(collectionName);
            var filters = Builders<T>.Filter.Eq(field, value);
            var options = new FindOneAndDeleteOptions<T>();
            await collection.FindOneAndDeleteAsync(filters, options);
        }

        public async Task DeleteOneAsync<T>(List<KeyValuePair<string, object>> filters, string collectionName) where T : ITEDocumentObject
        {
            if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));
            if (filters == null) throw new ArgumentNullException(nameof(filters));
            if (filters.Count < 0) throw new ArgumentException("The argument 'filters' requires at least one filter.");

            var collection = _db.GetCollection<T>(collectionName);
            List<FilterDefinition<T>> filterList = new List<FilterDefinition<T>>();
            foreach (var kvp in filters)
            {
                filterList.Add(Builders<T>.Filter.Eq(kvp.Key, kvp.Value));
            }
            var mongoFilters = Builders<T>.Filter.And(filterList);
            var options = new FindOneAndDeleteOptions<T>();
            await collection.FindOneAndDeleteAsync(mongoFilters, options);
        }

        public async Task<long> GetNextSequenceAsync(string name)
        {
            var collection = _db.GetCollection<TEMongoSequence>("sequences");
            var filter = Builders<TEMongoSequence>.Filter.Eq("Name", name);
            var findOneAndUpdateOptions = new FindOneAndUpdateOptions<TEMongoSequence>()
            {
                IsUpsert = false
            };
            var update = Builders<TEMongoSequence>.Update.Inc<long>("Sequence", 1);
            TEMongoSequence seq = await collection.FindOneAndUpdateAsync<TEMongoSequence>(filter, update, findOneAndUpdateOptions);
            if (seq == null)
            {
                seq = new TEMongoSequence()
                {
                    ObjectID = ObjectId.GenerateNewId().ToString(),
                    Sequence = 2,
                    Name = name
                };
                ReplaceOptions options = new ReplaceOptions()
                {
                    IsUpsert = true
                };
                ReplaceOneResult result = await collection.ReplaceOneAsync(filter, seq, options);
                seq.Sequence--;
            }

            return seq.Sequence;
        }

        public async Task ResetSequence(string name, long value)
        {
            var collection = _db.GetCollection<TEMongoSequence>("sequences");
            var filter = Builders<TEMongoSequence>.Filter.Eq("Name", name);
            var findOneAndUpdateOptions = new FindOneAndUpdateOptions<TEMongoSequence>()
            {
                IsUpsert = false
            };
            var update = Builders<TEMongoSequence>.Update.Set<long>("Sequence", value);
            TEMongoSequence seq = await collection.FindOneAndUpdateAsync<TEMongoSequence>(filter, update, findOneAndUpdateOptions);
            if (seq == null)
            {
                seq = new TEMongoSequence()
                {
                    ObjectID = ObjectId.GenerateNewId().ToString(),
                    Sequence = value,
                    Name = name
                };
                ReplaceOptions options = new ReplaceOptions()
                {
                    IsUpsert = true
                };
                ReplaceOneResult result = await collection.ReplaceOneAsync(filter, seq, options);
            }
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TEMongoDB()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
