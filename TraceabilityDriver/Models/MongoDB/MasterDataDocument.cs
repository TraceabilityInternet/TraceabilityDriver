using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace TraceabilityDriver.Models.MongoDB
{
    public class MasterDataDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string ElementId { get; set; } = string.Empty;

        public string ElementType { get; set; } = string.Empty;

        public string ElementJson { get; set; } = string.Empty;
    }
} 