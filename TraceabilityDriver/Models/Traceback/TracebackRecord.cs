using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraceabilityDriver.Models.Traceback
{
    /// <summary>
    /// A single traceback run: one HTTP-triggered pull of data from an external traceability server into the data cache.
    /// </summary>
    /// <remarks>
    /// Records are append-per-run — a rerun of the same EPCs creates a new record rather than mutating an old one,
    /// so the history of what was ingested and when is preserved. The events and master data the run touched are
    /// recorded as <see cref="TracebackItem"/> rows keyed to <see cref="Id"/>, giving operators a ledger they can
    /// use to sync ingested data back into their own systems. This model is shared by the MongoDB and SQL backends,
    /// following the same pattern as <see cref="MongoDB.SyncHistoryItem"/>.
    /// </remarks>
    public class TracebackRecord
    {
        /// <summary>
        /// The unique identifier of the traceback run.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        /// <summary>
        /// The UTC time the traceback started.
        /// </summary>
        public DateTime? StartTime { get; set; } = null;

        /// <summary>
        /// The UTC time the traceback finished, or null while it is still running.
        /// </summary>
        public DateTime? EndTime { get; set; } = null;

        /// <summary>
        /// The current lifecycle status of the run.
        /// </summary>
        public TracebackStatus Status { get; set; } = TracebackStatus.InProgress;

        /// <summary>
        /// The digital link resolver URL of the external server the traceback ran against.
        /// </summary>
        public string ResolverUrl { get; set; } = string.Empty;

        /// <summary>
        /// The EPCs the traceback was requested for.
        /// </summary>
        public List<string> RequestedEpcs { get; set; } = new List<string>();

        /// <summary>
        /// Errors collected during the run. A non-empty list with a finished run means <see cref="TracebackStatus.CompletedWithErrors"/>.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// The number of events this run added to the data cache.
        /// </summary>
        public int EventsCreated { get; set; }

        /// <summary>
        /// The number of events this run updated that already existed in the data cache.
        /// </summary>
        public int EventsUpdated { get; set; }

        /// <summary>
        /// The number of master data elements this run added to the data cache.
        /// </summary>
        public int MasterDataCreated { get; set; }

        /// <summary>
        /// The number of master data elements this run updated that already existed in the data cache.
        /// </summary>
        public int MasterDataUpdated { get; set; }
    }
}
