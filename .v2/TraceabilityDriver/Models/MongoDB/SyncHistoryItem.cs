namespace TraceabilityDriver.Models.MongoDB
{
    /// <summary>
    /// Represents an item in the synchronization history. It likely contains details about synchronization events.
    /// </summary>
    public class SyncHistoryItem
    {
        /// <summary>
        /// The time it started syncing.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The time it finished syncing.
        /// </summary>
        public DateTime? EndTime { get; set; } = null;

        /// <summary>
        /// Represents the current status of the sync.
        /// </summary>
        public string Status { get; set; } = "In Progress";

        /// <summary>
        /// The number of items in the database.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Represents the number of items that have been processed. It is an integer property that can be both
        /// retrieved and modified.
        /// </summary>
        public int ItemsProcessed { get; set; }

        /// <summary>
        /// Represents the number of events created. It is an integer property that can be both retrieved and modified.
        /// </summary>
        public int EventsCreated { get; set; }
    }
}
