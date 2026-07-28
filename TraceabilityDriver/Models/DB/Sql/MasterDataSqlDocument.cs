namespace TraceabilityDriver.Models.DB.Sql
{
    /// <summary>
    /// A master data row. Elements are unique per (element id, deployment version) so master data
    /// synced under previous deployment versions persists without being served. Traceback rows carry a
    /// null deployment version and are never updated once stored.
    /// </summary>
    public class MasterDataSqlDocument
    {
        public long ID { get; set; }

        public string ElementId { get; set; } = string.Empty;

        /// <summary>
        /// The deployment version the element was synced under. Null on traceback rows.
        /// </summary>
        public string? DeploymentVersion { get; set; } = null;

        public string ElementType { get; set; } = string.Empty;

        public string ElementJson { get; set; } = string.Empty;
    }
}
