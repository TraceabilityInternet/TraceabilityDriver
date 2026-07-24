namespace TraceabilityDriver.Models.MongoDB
{
    /// <summary>
    /// Reports which resources a store operation inserted and which it updated, keyed by their natural ids.
    /// </summary>
    /// <remarks>
    /// Returned by the event and master data store methods so callers such as the ingestion service can build a
    /// per-run ledger of created versus updated resources without issuing any additional queries.
    /// </remarks>
    public class DatabaseStoreResult
    {
        /// <summary>
        /// The natural ids (event id or element id) of resources that did not exist before and were inserted.
        /// </summary>
        public List<string> CreatedIds { get; set; } = new List<string>();

        /// <summary>
        /// The natural ids (event id or element id) of resources that already existed and were updated.
        /// </summary>
        public List<string> UpdatedIds { get; set; } = new List<string>();

        /// <summary>
        /// Merges another result into this one.
        /// </summary>
        /// <param name="other">The result to merge. Cannot be null.</param>
        public void Merge(DatabaseStoreResult other)
        {
            CreatedIds.AddRange(other.CreatedIds);
            UpdatedIds.AddRange(other.UpdatedIds);
        }
    }
}
