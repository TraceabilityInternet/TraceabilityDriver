using OpenTraceability.Queries;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// The normalized filter values of an EPCIS event query, precomputed once so every database
    /// backend translates the same semantics into its own query shape.
    /// </summary>
    /// <remarks>
    /// The search fields are stored lowercased, so every value here is lowercased to match. Record
    /// time bounds are converted to UTC <see cref="DateTime"/> because both backends store record
    /// time as a UTC <see cref="DateTime"/>; comparing a <see cref="DateTimeOffset"/> bound against
    /// those fields is what made the Mongo driver throw on record time queries. Business step values
    /// are expanded into every accepted CBV form (raw, urn, GS1 web vocabulary) the way the
    /// in-memory <c>EPCISBaseDocument.FilterEvents</c> reference implementation accepts them.
    /// </remarks>
    public class EventQueryFilterValues
    {
        private const string BizStepUrnPrefix = "urn:epcglobal:cbv:bizstep:";
        private const string BizStepWebVocabPrefix = "https://ref.gs1.org/cbv/bizstep-";

        /// <summary>
        /// The inclusive lower bound on the event time, or null when not filtered.
        /// </summary>
        public DateTimeOffset? GE_EventTime { get; private set; }

        /// <summary>
        /// The exclusive upper bound on the event time, or null when not filtered.
        /// </summary>
        public DateTimeOffset? LT_EventTime { get; private set; }

        /// <summary>
        /// The inclusive lower bound on the record time in UTC, or null when not filtered.
        /// </summary>
        public DateTime? GE_RecordTimeUtc { get; private set; }

        /// <summary>
        /// The exclusive upper bound on the record time in UTC, or null when not filtered.
        /// </summary>
        public DateTime? LT_RecordTimeUtc { get; private set; }

        /// <summary>
        /// The MATCH_anyEPC values, matched against the EPCs of all of an event's products.
        /// </summary>
        public EpcMatchValues MatchAnyEpc { get; private set; } = new EpcMatchValues();

        /// <summary>
        /// The MATCH_anyEPCClass values, matched against the EPCs of all of an event's products.
        /// </summary>
        public EpcMatchValues MatchAnyEpcClass { get; private set; } = new EpcMatchValues();

        /// <summary>
        /// The MATCH_epc values, matched only against the EPCs of an event's reference and child products.
        /// </summary>
        public EpcMatchValues MatchEpc { get; private set; } = new EpcMatchValues();

        /// <summary>
        /// The MATCH_epcClass values, matched only against the EPCs of an event's reference and child products.
        /// </summary>
        public EpcMatchValues MatchEpcClass { get; private set; } = new EpcMatchValues();

        /// <summary>
        /// The lowercased EQ_bizStep candidates, with each query value expanded into every accepted
        /// CBV form so it matches regardless of the form the event was stored with.
        /// </summary>
        public List<string> BizSteps { get; } = new List<string>();

        /// <summary>
        /// The lowercased EQ_action values.
        /// </summary>
        public List<string> Actions { get; } = new List<string>();

        /// <summary>
        /// The lowercased EQ_bizLocation values.
        /// </summary>
        public List<string> BizLocations { get; } = new List<string>();

        /// <summary>
        /// The lowercased eventTypes values (e.g. "objectevent").
        /// </summary>
        public List<string> EventTypes { get; } = new List<string>();

        /// <summary>
        /// The lowercased EQ_transformationID values. Only transformation events carry a
        /// transformation id, so every other event is excluded when this filter is present.
        /// </summary>
        public List<string> TransformationIds { get; } = new List<string>();

        /// <summary>
        /// Normalizes the given EPCIS query parameters into backend-agnostic filter values.
        /// </summary>
        /// <param name="options">The EPCIS query parameters as received by the query endpoint.</param>
        /// <returns>The normalized filter values.</returns>
        public static EventQueryFilterValues Create(EPCISQueryParameters options)
        {
            EventQueryFilterValues values = new EventQueryFilterValues();

            values.GE_EventTime = options.query.GE_eventTime;
            values.LT_EventTime = options.query.LT_eventTime;
            values.GE_RecordTimeUtc = options.query.GE_recordTime?.UtcDateTime;
            values.LT_RecordTimeUtc = options.query.LT_recordTime?.UtcDateTime;

            values.MatchAnyEpc = EpcMatchValues.Create(options.query.MATCH_anyEPC);
            values.MatchAnyEpcClass = EpcMatchValues.Create(options.query.MATCH_anyEPCClass);
            values.MatchEpc = EpcMatchValues.Create(options.query.MATCH_epc);
            values.MatchEpcClass = EpcMatchValues.Create(options.query.MATCH_epcClass);

            foreach (string bizStep in options.query.EQ_bizStep.Where(b => !string.IsNullOrWhiteSpace(b)))
            {
                values.BizSteps.AddRange(ExpandBizStep(bizStep));
            }

            values.Actions.AddRange(options.query.EQ_action.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim().ToLower()));
            values.BizLocations.AddRange(options.query.EQ_bizLocation.Select(l => l.ToString().ToLower()));
            values.EventTypes.AddRange(options.query.eventTypes.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLower()));
            values.TransformationIds.AddRange(options.query.EQ_transformationID.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLower()));

            return values;
        }

        /// <summary>
        /// Expands one EQ_bizStep query value into every accepted CBV form it could be stored as.
        /// </summary>
        /// <remarks>
        /// EPCIS accepts a business step as a short CBV name ("commissioning"), a CBV urn, or a GS1
        /// web vocabulary URI. Events are stored with whichever form they arrived in, so the short
        /// name is expanded into the urn and web vocabulary forms as well. Values that are absolute
        /// URIs outside the CBV namespaces (e.g. GDST business steps) are kept as-is.
        /// </remarks>
        private static List<string> ExpandBizStep(string bizStep)
        {
            string normalized = bizStep.Trim().ToLower();

            // Determine the short CBV name when the value is in one of the CBV forms.
            string? shortName = null;
            if (normalized.StartsWith(BizStepUrnPrefix))
            {
                shortName = normalized.Substring(BizStepUrnPrefix.Length);
            }
            else if (normalized.StartsWith(BizStepWebVocabPrefix))
            {
                shortName = normalized.Substring(BizStepWebVocabPrefix.Length);
            }
            else if (!Uri.TryCreate(normalized, UriKind.Absolute, out _))
            {
                shortName = normalized;
            }

            List<string> candidates = new List<string> { normalized };
            if (shortName != null)
            {
                candidates.Add(shortName);
                candidates.Add(BizStepUrnPrefix + shortName);
                candidates.Add(BizStepWebVocabPrefix + shortName);
            }

            return candidates.Distinct().ToList();
        }
    }
}
