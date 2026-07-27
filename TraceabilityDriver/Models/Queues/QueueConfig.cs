namespace TraceabilityDriver.Models.Queues
{
    /// <summary>
    /// Configuration for the background job queue's storage backend.
    /// </summary>
    public class QueueConfig
    {
        /// <summary>
        /// The storage backend the queue persists jobs into.
        /// </summary>
        public QueueStoreType StoreType { get; set; } = QueueStoreType.SqlServer;

        /// <summary>
        /// The connection string of the queue storage database. Required.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// The database name to store jobs in. Mongo only; when null, the database name is parsed from the connection string.
        /// </summary>
        public string? DatabaseName { get; set; }
    }
}
