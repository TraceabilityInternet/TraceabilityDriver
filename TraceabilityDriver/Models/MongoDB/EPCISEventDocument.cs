using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.MasterData;
using System;
using System.Collections.Generic;

namespace TraceabilityDriver.Models.MongoDB
{
    public class EPCISEventDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string EventId { get; set; } = string.Empty;

        public string EventJson { get; set; } = string.Empty;

        public string BizStep { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public DateTimeOffset? EventTime { get; set; }

        public DateTime RecordTime { get; set; } = DateTime.UtcNow;

        public DateTime AuditTime { get; set; } = DateTime.UtcNow;

        public List<string> EPCs { get; set; } = new List<string>();

        public List<string> ProductGTINs { get; set; } = new List<string>();

        public List<string> LocationGLNs { get; set; } = new List<string>();

        public List<string> PartyPGLNs { get; set; } = new List<string>();
    }
}