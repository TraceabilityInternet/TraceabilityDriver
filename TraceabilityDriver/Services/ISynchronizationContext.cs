using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Models.MongoDB;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Provides context to the current mapping being processed.
    /// </summary>
    public interface ISynchronizationContext
    {
        /// <summary>
        /// Represents the configuration settings for TDMapping. It can be null, indicating no configuration is set.
        /// </summary>
        TDMappingConfiguration? Configuration { get; set; }

        /// <summary>
        /// Represents the previous synchronization item in a sync history. It can be null if no previous sync exists.
        /// </summary>
        SyncHistoryItem? PreviousSync { get; set; }

        /// <summary>
        /// Represents the current synchronization item. It provides access to the details of the ongoing sync process.
        /// </summary>
        SyncHistoryItem CurrentSync { get; set; }
    
        /// <summary>
        /// An event that is triggered when the synchronization status changes. It allows subscribers to respond to
        /// status updates.
        /// </summary>
        event OnSynchronizeStatusChanged? OnSynchronizeStatusChanged;

        /// <summary>
        /// Updates the state or data of an object. Typically called to refresh or synchronize information.
        /// </summary>
        void Updated();
    }
}
