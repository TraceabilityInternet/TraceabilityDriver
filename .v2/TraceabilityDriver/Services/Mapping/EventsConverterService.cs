using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using TraceabilityDriver.Models;
using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services;

/// <summary>
/// The service for converting common events to EPCIS events.
/// </summary>
public class EventsConverterService : IEventsConverterService
{
    private readonly ILogger<EventsConverterService> _logger;

    public EventsConverterService(ILogger<EventsConverterService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Converts the common events to EPCIS events.
    /// </summary>
    public Task<EPCISDocument> ConvertEventsAsync(List<CommonEvent> events)
    {
        EPCISDocument doc = new EPCISDocument();

        foreach (var commonEvent in events)
        {
            if (!IsEventValid(commonEvent))
            {
                _logger.LogError("Event is not valid for conversion: {EventId}", commonEvent.EventId);
                continue;
            }

            switch (commonEvent.EventType?.Trim().ToLower())
            {
                case "gdstfishingevent": ConvertTo_GDSTFishingEvent(commonEvent, doc); break;
            }
        }

        return Task.FromResult(doc);
    }

    /// <summary>
    /// Determines if the event is valid for converting to an EPCIS event.
    /// </summary>
    /// <param name="commonEvent">The event to validate.</param>
    /// <returns>TRUE if the event is valid, otherwise FALSE.</returns>
    public bool IsEventValid(CommonEvent commonEvent)
    {
        return true;
    }

    /// <summary>
    /// Converts the common event to a GDST Fishing Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTFishingEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTFishingEvent epcisEvent = new GDSTFishingEvent
        {

        };

        doc.Events.Add(epcisEvent);
    }
}

