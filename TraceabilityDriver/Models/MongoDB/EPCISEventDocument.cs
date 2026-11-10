using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Models.MongoDB
{
    /// <summary>
    /// A stored EPCIS event in the Mongo GDST data cache, holding the event JSON alongside its indexed search fields.
    /// </summary>
    public class EPCISEventDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string EventId { get; set; } = string.Empty;

        public string EventJson { get; set; } = string.Empty;

        public string BizStep { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public DateTimeOffset? EventTime { get; set; }

        public DateTime RecordTime { get; set; } = DateTime.UtcNow;

        public DateTime AuditTime { get; set; } = DateTime.UtcNow;

        public List<string> EPCs { get; set; } = new List<string>();

        public List<string> ProductGTINs { get; set; } = new List<string>();

        /// <summary>
        /// The lowercased business location GLN of the event; this is the field <c>EQ_bizLocation</c> filters on.
        /// </summary>
        /// <remarks>
        /// Always written for new documents, as BSON null when the event has no business location. A document
        /// where the field is missing entirely was stored before it existed and is backfilled at startup.
        /// </remarks>
        public string? BizLocationGLN { get; set; }

        /// <summary>
        /// The business location GLN plus every source and destination location GLN of the event.
        /// </summary>
        public List<string> LocationGLNs { get; set; } = new List<string>();

        public List<string> PartyPGLNs { get; set; } = new List<string>();

        /// <summary>
        /// Creates an empty document, used by the Mongo serializer.
        /// </summary>
        public EPCISEventDocument()
        {
        }

        /// <summary>
        /// Creates a document for the event, serializing it to JSON and extracting its search fields.
        /// </summary>
        /// <param name="evt">The event to store. Cannot be null.</param>
        public EPCISEventDocument(IEvent evt)
        {
            EPCISQueryDocument doc = new EPCISQueryDocument();
            doc.EPCISVersion = EPCISVersion.V2;
            doc.Header = OpenTraceability.Models.Common.StandardBusinessDocumentHeader.DummyHeader;
            doc.Events.Add(evt);

            EventSearchFields fields = EventSearchFields.FromEvent(evt);

            EventId = evt.EventID.ToString();
            EventJson = OpenTraceability.Mappers.OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(doc);
            BizStep = fields.BizStep;
            Action = fields.Action;
            EventTime = fields.EventTime;
            EPCs = fields.EPCs;
            ProductGTINs = fields.ProductGTINs;
            BizLocationGLN = fields.BizLocationGLN;
            LocationGLNs = fields.LocationGLNs;
            PartyPGLNs = fields.PartyPGLNs;
        }
    }
}
