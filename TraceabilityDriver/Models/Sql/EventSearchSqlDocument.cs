using OpenTraceability.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace TraceabilityDriver.Models.Sql
{
    /// <summary>
    /// A model class for entity framework for saving searchable fields into the SQL server
    /// in a way that they can also be indexed.
    /// </summary>
    public class EventSearchSqlDocument
    {
        [Key]
        public long ID { get; set; }

        /// <summary>
        /// The event ID that is being indexed.
        /// </summary>
        public string EventId { get; set; } = string.Empty;

        /// <summary>
        /// Represents the business step associated with a process. Initialized to an empty string.
        /// </summary>
        public string BizStep { get; set; } = string.Empty;

        /// <summary>
        /// Represents an action as a string. Initialized to an empty string by default.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Represents the time when an event occurred, stored as a nullable DateTimeOffset. If no event time is set, it
        /// can be null.
        /// </summary>
        public DateTimeOffset? EventTime { get; set; }

        /// <summary>
        /// Represents the time when a record was created, initialized to the current UTC time.
        /// </summary>
        public DateTime RecordTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// EPC is a string property that holds a value related to an Electronic Product Code. It is initialized to an
        /// empty string.
        /// </summary>
        public string EPC { get; set; } = string.Empty;

        /// <summary>
        /// Represents the Global Trade Item Number (GTIN) of a product. Initialized to an empty string by default.
        /// </summary>
        public string ProductGTIN { get; set; } = string.Empty;

        /// <summary>
        /// Represents the Global Location Number (GLN) for a location. Initialized to an empty string.
        /// </summary>
        public string LocationGLN { get; set; } = string.Empty;

        /// <summary>
        /// Represents the Party PGLN as a string. Initialized to an empty string by default.
        /// </summary>
        public string PartyPGLN { get; set; } = string.Empty;

        /// <summary>
        /// The lowercased business location GLN of the event, repeated on every search row of the event; this is the
        /// column <c>EQ_bizLocation</c> filters on.
        /// </summary>
        /// <remarks>
        /// New rows store an empty string when the event has no business location. <see langword="null"/> marks a row
        /// written before this column existed; such rows are backfilled from the stored event JSON at startup.
        /// </remarks>
        public string? BizLocationGLN { get; set; }

        /// <summary>
        /// Creates the search rows for the events, pivoting each event's multi-valued fields across as many rows as its longest list.
        /// </summary>
        /// <param name="evts">The events to index.</param>
        /// <returns>The search rows for all of the events.</returns>
        public static List<EventSearchSqlDocument> CreateSearchDocuments(List<IEvent> evts)
        {
            List<EventSearchSqlDocument> searchDocument = new List<EventSearchSqlDocument>();

            foreach (IEvent evt in evts)
            {
                EventSearchFields fields = EventSearchFields.FromEvent(evt);

                // Get the highest index to iterate through
                int maxCount = Math.Max(Math.Max(Math.Max(fields.EPCs.Count, fields.ProductGTINs.Count), fields.LocationGLNs.Count), fields.PartyPGLNs.Count);

                for (int i = 0; i < maxCount; i++)
                {
                    EventSearchSqlDocument searchDoc = new EventSearchSqlDocument
                    {
                        EventId = evt.EventID.ToString(),
                        BizStep = fields.BizStep,
                        Action = fields.Action,
                        EventTime = fields.EventTime,
                        EPC = i < fields.EPCs.Count ? fields.EPCs[i] : string.Empty,
                        ProductGTIN = i < fields.ProductGTINs.Count ? fields.ProductGTINs[i] : string.Empty,
                        LocationGLN = i < fields.LocationGLNs.Count ? fields.LocationGLNs[i] : string.Empty,
                        PartyPGLN = i < fields.PartyPGLNs.Count ? fields.PartyPGLNs[i] : string.Empty,
                        BizLocationGLN = fields.BizLocationGLN ?? string.Empty
                    };
                    searchDocument.Add(searchDoc);
                }
            }

            return searchDocument;
        }
    }
}
