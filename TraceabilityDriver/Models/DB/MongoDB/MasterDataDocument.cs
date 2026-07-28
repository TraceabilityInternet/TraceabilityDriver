using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace TraceabilityDriver.Models.DB.MongoDB
{
    public class MasterDataDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string ElementId { get; set; } = string.Empty;

        /// <summary>
        /// The deployment version the element was synced under. Null on documents stored in the
        /// traceback master data collection, which is not versioned.
        /// </summary>
        public string? DeploymentVersion { get; set; }

        public string ElementType { get; set; } = string.Empty;

        public string ElementJson { get; set; } = string.Empty;
    }
} 