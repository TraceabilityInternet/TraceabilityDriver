using Newtonsoft.Json.Linq;
using TraceabilityDriver.Models.Mapping;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TraceabilityDriver.Services;

/// <summary>
/// An implementation of the events merger that merges events together by their ID. Events are assumed
/// to come in order of priority, such that an event with EventId "1" first in the list will have it's
/// property values prioritized over an event with EventID "1" later in the list. Such that, if the first
/// event sets the "Location.Name" to "ABC Inc." and the second sets it to "ABC", the merged event will
/// have the name "ABC Inc.".
/// </summary>
public class EventsMergeByIdService : IEventsMergerService
{
    private readonly ILogger<EventsMergeByIdService> _logger;

    public EventsMergeByIdService(ILogger<EventsMergeByIdService> logger)
    {
        _logger = logger;
    }

    public Task<List<CommonEvent>> MergeEventsAsync(TDMapping mapping, List<CommonEvent> events)
    {
        List<CommonEvent> mergedEvents = new List<CommonEvent>();

        // This merges events together by grouping by their "EventId"
        foreach (var group in events.GroupBy(g => g.EventId))
        {
            // If there is only one event, then there is no need to merge it.
            if (group.Count() == 1)
            {
                mergedEvents.Add(group.First());
                continue;
            }

            // Start with the base of the first event in the group.
            CommonEvent evt = group.First();

            // Merge the other events into the first event.
            foreach (CommonEvent other in group.Skip(1))
            {
                evt.Merge(other);
            }

            mergedEvents.Add(evt);
        }

        return Task.FromResult(mergedEvents);
    }
}

