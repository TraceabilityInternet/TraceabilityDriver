using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Models
{
    /// <summary>
    /// The searchable fields extracted from an EPCIS event, shared by every GDST data cache backend.
    /// </summary>
    /// <remarks>
    /// Each cache backend indexes a handful of event fields so that EPCIS query parameters can be
    /// answered without deserializing every stored event. Extraction lives here, once, so the
    /// backends cannot drift apart in how they interpret an event.
    /// <para>
    /// <see cref="BizLocationGLN"/> holds only the event's business location and is what the
    /// <c>EQ_bizLocation</c> query parameter filters on. <see cref="LocationGLNs"/> additionally holds
    /// source and destination location GLNs; it is kept for a future source/destination filter and
    /// must not be used to answer <c>EQ_bizLocation</c>.
    /// </para>
    /// </remarks>
    public class EventSearchFields
    {
        /// <summary>
        /// The lowercased business step of the event.
        /// </summary>
        public string BizStep { get; set; } = string.Empty;

        /// <summary>
        /// The lowercased action of the event, or an empty string when the event has none.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// The time the event occurred.
        /// </summary>
        public DateTimeOffset? EventTime { get; set; }

        /// <summary>
        /// The lowercased EPCs of every product on the event.
        /// </summary>
        public List<string> EPCs { get; set; } = new List<string>();

        /// <summary>
        /// The lowercased GTINs of every product on the event that carries one.
        /// </summary>
        public List<string> ProductGTINs { get; set; } = new List<string>();

        /// <summary>
        /// The lowercased GLN of the event's business location, or <see langword="null"/> when the event has no business location.
        /// </summary>
        public string? BizLocationGLN { get; set; }

        /// <summary>
        /// The business location GLN followed by every source and destination location GLN on the event.
        /// </summary>
        public List<string> LocationGLNs { get; set; } = new List<string>();

        /// <summary>
        /// The PGLNs of the trading parties on the event: the GDST product owner and information provider,
        /// then every owning or possessing party in the source and destination lists.
        /// </summary>
        public List<string> PartyPGLNs { get; set; } = new List<string>();

        /// <summary>
        /// Extracts the searchable fields from the event.
        /// </summary>
        /// <param name="evt">The event to extract from. Cannot be null.</param>
        /// <returns>The searchable fields of the event.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="evt"/> is null.</exception>
        public static EventSearchFields FromEvent(IEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            string? bizLocationGLN = GetBizLocationGLN(evt);

            EventSearchFields fields = new EventSearchFields
            {
                BizStep = evt.BusinessStep.ToString().ToLower(),
                Action = evt.Action.ToString()?.ToLower() ?? "",
                EventTime = evt.EventTime,
                EPCs = evt.Products.Select(p => p.EPC.ToString().ToLower()).ToList(),
                ProductGTINs = evt.Products.Select(p => p.EPC.GTIN?.ToString().ToLower()).Where(g => g != null).Select(g => g!).ToList(),
                BizLocationGLN = bizLocationGLN,
                LocationGLNs = bizLocationGLN != null ? new List<string> { bizLocationGLN } : new List<string>()
            };

            // GDST events name their trading parties explicitly, ahead of the source/destination parties.
            if (evt is IGDSTEvent gdstEvent)
            {
                if (gdstEvent.ProductOwner != null)
                {
                    fields.PartyPGLNs.Add(gdstEvent.ProductOwner.ToString());
                }
                if (gdstEvent.InformationProvider != null)
                {
                    fields.PartyPGLNs.Add(gdstEvent.InformationProvider.ToString());
                }
            }

            // Sources and destinations are split by type: parties into PartyPGLNs, locations into LocationGLNs.
            foreach (EventSource source in evt.SourceList)
            {
                fields.AddSourceOrDestination(source.Type, source.Value);
            }

            foreach (EventDestination destination in evt.DestinationList)
            {
                fields.AddSourceOrDestination(destination.Type, destination.Value);
            }

            return fields;
        }

        /// <summary>
        /// Extracts the business location GLN from a stored event JSON document.
        /// </summary>
        /// <remarks>
        /// Used to backfill caches written before <see cref="BizLocationGLN"/> was stored separately; at that
        /// time the business location could not be told apart from source/destination locations, so the
        /// stored event JSON is the only reliable source. The document is expected to hold a single event.
        /// </remarks>
        /// <param name="eventJson">An EPCIS query document in JSON holding the stored event.</param>
        /// <returns>The lowercased business location GLN, or <see langword="null"/> when the event has no business location.</returns>
        /// <exception cref="Exception">Thrown when the JSON cannot be parsed as an EPCIS query document.</exception>
        public static string? ExtractBizLocationGLN(string eventJson)
        {
            EPCISQueryDocument doc = OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(eventJson);
            IEvent? evt = doc.Events.FirstOrDefault();
            return evt != null ? GetBizLocationGLN(evt) : null;
        }

        /// <summary>
        /// Gets the lowercased business location GLN of the event, or null when it has none.
        /// </summary>
        private static string? GetBizLocationGLN(IEvent evt)
        {
            return evt.Location?.GLN?.ToString().ToLower();
        }

        /// <summary>
        /// Records a source or destination entry as a party or a location according to its type; other types are ignored.
        /// </summary>
        private void AddSourceOrDestination(Uri? type, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (type == OpenTraceability.Constants.EPCIS.URN.SDT_Possessor || type == OpenTraceability.Constants.EPCIS.URN.SDT_Owner)
            {
                PartyPGLNs.Add(value);
            }
            else if (type == OpenTraceability.Constants.EPCIS.URN.SDT_Location)
            {
                LocationGLNs.Add(value);
            }
        }
    }
}
