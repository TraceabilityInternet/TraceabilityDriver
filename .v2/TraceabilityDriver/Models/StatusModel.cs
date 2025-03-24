namespace TraceabilityDriver.Models
{
    /// <summary>
    /// Represents the status of an entity. It serves as a data model for managing status-related information.
    /// </summary>
    public class StatusModel
    {
        /// <summary>
        /// Indicates whether the Traceability Driver is currently synchronizing.
        /// </summary>
        public bool IsSynchronizing { get; set; } = false;
    }
}
