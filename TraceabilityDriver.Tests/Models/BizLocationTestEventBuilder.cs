using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Identifiers;

namespace TraceabilityDriver.Tests.Models
{
    /// <summary>
    /// Builds events for testing how the business location is indexed and queried apart from source and destination locations.
    /// </summary>
    /// <remarks>
    /// The shared test data file has no events with location-typed sources or destinations, so these scenarios are
    /// built in code. Events use the <c>inspecting</c> business step, which the shared test data does not contain, so
    /// adding them to a cache does not disturb the per-business-step counts other tests assert on.
    /// </remarks>
    public static class BizLocationTestEventBuilder
    {
        /// <summary>
        /// The business step given to every built event.
        /// </summary>
        public const string BizStep = "urn:epcglobal:cbv:bizstep:inspecting";

        /// <summary>
        /// The EPCIS source/destination type for a location.
        /// </summary>
        public const string LocationSdt = "urn:epcglobal:cbv:sdt:location";

        /// <summary>
        /// Creates a GLN unique to the calling test run, so query results are not polluted by other stored events.
        /// </summary>
        /// <returns>A GLN in SGLN URN form.</returns>
        public static string CreateUniqueGLN()
        {
            int locationRef = Random.Shared.Next(10000, 99999);
            int extension = Random.Shared.Next(1, 999999);
            return $"urn:epc:id:sgln:0614141.{locationRef}.{extension}";
        }

        /// <summary>
        /// Creates an observe event with the given business location and optional source and destination locations.
        /// </summary>
        /// <param name="bizLocationGLN">The business location GLN, or null for an event without a business location.</param>
        /// <param name="sourceLocationGLN">An optional location-typed source GLN.</param>
        /// <param name="destinationLocationGLN">An optional location-typed destination GLN.</param>
        /// <returns>The event, with a unique event ID.</returns>
        public static IEvent CreateEvent(string? bizLocationGLN, string? sourceLocationGLN = null, string? destinationLocationGLN = null)
        {
            ObjectEvent<EventILMD> evt = new ObjectEvent<EventILMD>
            {
                EventID = new Uri($"urn:uuid:{Guid.NewGuid()}"),
                EventTime = DateTimeOffset.UtcNow,
                EventTimeZoneOffset = TimeSpan.Zero,
                BusinessStep = new Uri(BizStep),
                Action = EventAction.OBSERVE
            };

            if (bizLocationGLN != null)
            {
                evt.Location = new EventLocation(new GLN(bizLocationGLN));
            }

            if (sourceLocationGLN != null)
            {
                evt.SourceList.Add(new EventSource { Type = new Uri(LocationSdt), Value = sourceLocationGLN });
            }

            if (destinationLocationGLN != null)
            {
                evt.DestinationList.Add(new EventDestination { Type = new Uri(LocationSdt), Value = destinationLocationGLN });
            }

            return evt;
        }
    }
}
