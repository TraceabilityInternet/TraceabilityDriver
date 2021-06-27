using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Interfaces.DB.DocumentDB
{
    public enum TEDocumentDatabaseType
    {
        Mongo = 0
    };

    public interface ITEDocumentDB : IDisposable
    {
        public Task SaveAsync<T>(T obj, string table) where T : ITEDocumentObject;

        public Task SaveAsync<T>(T obj, string field, object value, string table) where T : ITEDocumentObject;

        public Task UpdateField<T>(string matchField, object matchValue, string field, object value, string table) where T : ITEDocumentObject;

        public Task<T> LoadAsync<T>(string ID, string table) where T : ITEDocumentObject;

        public Task<T> LoadAsync<T>(string field, object value, string table) where T : ITEDocumentObject;

        public Task<T> LoadAsync<T>(List<KeyValuePair<string,object>> filters, string table) where T : ITEDocumentObject;

        public Task<bool> Exists<T>(string field, object value, string table) where T : ITEDocumentObject;

        public Task<List<T>> LoadManyAsync<T, T2>(string field, List<T2> values, string table) where T : ITEDocumentObject;

        public Task<List<T>> LoadAll<T>(string collectionName) where T : ITEDocumentObject;

        public Task DropAsync(string table);

        public Task DeleteOneAsync<T>(string field, object value, string table) where T : ITEDocumentObject;
        public Task DeleteOneAsync<T>(List<KeyValuePair<string, object>> filters, string table) where T : ITEDocumentObject;

        public Task<long> GetNextSequenceAsync(string sequence);

        public Task ResetSequence(string name, long value);
    }
}
