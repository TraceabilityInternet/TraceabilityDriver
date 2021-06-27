using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Services.DirectoryService;

namespace DirectoryService.DB.Serializers
{
    public class TEDirectoryAccountSerializer : IBsonSerializer<ITEDirectoryAccount>
    {
        public Type ValueType => throw new NotImplementedException();

        public ITEDirectoryAccount Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            throw new NotImplementedException();
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ITEDirectoryAccount value)
        {
            throw new NotImplementedException();
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            throw new NotImplementedException();
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
