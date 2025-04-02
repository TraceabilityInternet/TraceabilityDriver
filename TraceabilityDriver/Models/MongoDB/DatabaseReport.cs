namespace TraceabilityDriver.Models.MongoDB
{
    /// <summary>
    /// A report on the data in the database for reporting purposes.
    /// </summary>
    public class DatabaseReport
    {
        /// <summary>
        /// Contains a dictionary of the events grouped by their event type and the number of counts of each.
        /// </summary>
        public SortedDictionary<string, int> EventCounts { get; set; } = new();

        /// <summary>
        /// A dictionary that holds counts of master data items, with the key representing the type of 
        /// master data item and the value is the number of that master data item type.
        /// </summary>
        public SortedDictionary<string, int> MasterDataCounts { get; set; } = new();

        /// <summary>
        /// A dictionary that holds the number of syncs performed with the key being the final result
        /// of that sync either completed or failed, and the value being the number of that syncs.
        /// </summary>
        public SortedDictionary<SyncStatus, int> SyncCounts { get; set; } = new();
    }
}
