using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraceabilityDriver.Models.Traceback
{
    /// <summary>
    /// A ledger entry recording one event or master data element that a traceback run created or updated in the data cache.
    /// </summary>
    /// <remarks>
    /// Rows are unique per (TracebackId, ItemType, ItemId), so retrying a run's ledger write can never duplicate
    /// entries. Operators query these rows to see exactly what a traceback ingested and sync that data into their
    /// internal database themselves — the driver never writes to the internal database.
    /// </remarks>
    public class TracebackItem
    {
        /// <summary>
        /// The unique identifier of the ledger entry.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        /// <summary>
        /// The id of the <see cref="TracebackRecord"/> this entry belongs to.
        /// </summary>
        public string TracebackId { get; set; } = string.Empty;

        /// <summary>
        /// Whether the item is an event or a master data element.
        /// </summary>
        public TracebackItemType ItemType { get; set; }

        /// <summary>
        /// The event id or master data element id of the item in the data cache.
        /// </summary>
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        /// True when the run inserted the item into the cache; false when the item already existed and was updated.
        /// </summary>
        public bool Created { get; set; }

        /// <summary>
        /// The UTC time the item was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
