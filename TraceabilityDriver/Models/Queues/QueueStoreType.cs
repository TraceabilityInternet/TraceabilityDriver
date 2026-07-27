namespace TraceabilityDriver.Models.Queues
{
    /// <summary>
    /// The storage backend used to persist queued background jobs.
    /// </summary>
    public enum QueueStoreType
    {
        /// <summary>
        /// Jobs are stored in a SQL Server database.
        /// </summary>
        SqlServer,

        /// <summary>
        /// Jobs are stored in a MongoDB database.
        /// </summary>
        Mongo
    }
}
