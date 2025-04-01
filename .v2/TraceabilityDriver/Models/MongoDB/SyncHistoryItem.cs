namespace TraceabilityDriver.Models.MongoDB
{
    /// <summary>
    /// Represents the status of a synchronization process. Possible values are InProgress, Completed, and Failed.
    /// </summary>
    public enum SyncStatus
    {
        InProgress,
        Completed,
        Failed
    }

    /// <summary>
    /// Represents an item in the synchronization history. It likely contains details about synchronization events.
    /// </summary>
    public class SyncHistoryItem
    {
        /// <summary>
        /// The time it started syncing.
        /// </summary>
        public DateTime? StartTime { get; set; } = null;

        /// <summary>
        /// The time it finished syncing.
        /// </summary>
        public DateTime? EndTime { get; set; } = null;

        /// <summary>
        /// Represents the current status of the sync.
        /// </summary>
        public SyncStatus Status { get; set; } = SyncStatus.InProgress;

        /// <summary>
        /// A message that can be used to describe the sync.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Represents information captured from the previous sync.
        /// </summary>
        public Dictionary<string, string> Memory { get; set; } = new Dictionary<string, string>();

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
