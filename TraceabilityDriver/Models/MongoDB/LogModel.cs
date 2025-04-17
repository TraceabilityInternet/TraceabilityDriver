using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraceabilityDriver.Models.MongoDB
{
    public class LogModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("Timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; }

        [BsonElement("Level")]
        public string Level { get; set; } = null!;

        [BsonElement("RenderedMessage")]
        public string Message { get; set; } = null!;
    }
}
