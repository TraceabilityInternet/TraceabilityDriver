using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// A service for merging partial events from multiple sources together.
    /// </summary>
    public interface IEventsMergerService
    {
        /// <summary>
        /// Merges the events together.
        /// </summary>
        /// <param name="mapping">The mapping configuration used to generate the events.</param>
        /// <param name="events">The events that will be merged together.</param>
        /// <returns>The merged events.</returns>
        Task<List<CommonEvent>> MergeEventsAsync(TDMapping mapping, List<CommonEvent> events);
    }
}