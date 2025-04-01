using System.Threading;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Models.MongoDB;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Holds the configuration and mapping that is currently being processed.
    /// </summary>
    public class SynchronizationContext : ISynchronizationContext
    {
        /// <summary>
        /// An event that is triggered when the synchronization status changes. It allows subscribers to respond to
        /// status updates.
        /// </summary>
        public event OnSynchronizeStatusChanged? OnSynchronizeStatusChanged;

        /// <summary>
        /// The current mapping configuration file being processed.
        /// </summary>
        public TDMappingConfiguration? Configuration { get; set; } = null;

        /// <summary>
        /// Information from the last time the sync took place.
        /// </summary>
        public SyncHistoryItem? PreviousSync { get; set; } = null;

        /// <summary>
        /// Information about the current sync taking place.
        /// </summary>
        public SyncHistoryItem CurrentSync { get; set; } = new();

        public SynchronizationContext()
        {

        }

        /// <summary>
        /// Triggers the OnSynchronizeStatusChanged event, passing the current instance and the CurrentSync status. This
        /// notifies subscribers of a status change.
        /// </summary>
        public void Updated()
        {
            OnSynchronizeStatusChanged?.Invoke(CurrentSync);
        }
    }
}
