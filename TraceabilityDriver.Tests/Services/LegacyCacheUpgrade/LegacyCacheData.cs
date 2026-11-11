using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services.LegacyCacheUpgrade
{
    /// <summary>
    /// Loads legacy cache snapshots and computes the query results a correctly upgraded cache must return.
    /// </summary>
    /// <remarks>
    /// The snapshots in <c>Data\LegacyCache</c> were written by the released driver code itself (v1.0.1 for MongoDB, the last
    /// build before the bizLocation index for SQL Server), so they reproduce the exact storage format existing deployments hold.
    /// Expected <c>EQ_bizLocation</c> results come from OpenTraceability's own in-memory <see cref="EPCISBaseDocument.FilterEvents"/>
    /// over the stored event JSON, an oracle independent of the driver's indexing code.
    /// </remarks>
    public static class LegacyCacheData
    {
        /// <summary>
        /// A GLN that appears nowhere in the snapshots, used to prove unknown locations match nothing.
        /// </summary>
        public const string AbsentGLN = "urn:epc:id:sgln:0000000.00000.0";

        /// <summary>
        /// Reads a snapshot file from the test output's <c>Data\LegacyCache</c> folder.
        /// </summary>
        /// <param name="fileName">The snapshot file name.</param>
        /// <returns>The file contents.</returns>
        public static string ReadFile(string fileName)
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "LegacyCache", fileName);
            if (!File.Exists(path))
            {
                Assert.Fail($"Legacy cache snapshot not found at {path}.");
            }
            return File.ReadAllText(path);
        }

        /// <summary>
        /// Parses stored single-event JSON documents into one document holding every event.
        /// </summary>
        /// <param name="eventJsons">The stored event JSON of each cached event.</param>
        /// <returns>A query document containing all of the events.</returns>
        public static EPCISQueryDocument ParseStoredEvents(IEnumerable<string> eventJsons)
        {
            EPCISQueryDocument all = new EPCISQueryDocument();
            foreach (string eventJson in eventJsons)
            {
                all.Events.AddRange(OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(eventJson).Events);
            }
            return all;
        }

        /// <summary>
        /// Gets every GLN worth querying: each business location, each source and destination value, and one absent GLN.
        /// </summary>
        /// <param name="storedEvents">The events in the cache.</param>
        /// <returns>The distinct GLNs, ordered for stable failure output.</returns>
        public static List<string> GetCandidateGLNs(EPCISQueryDocument storedEvents)
        {
            HashSet<string> glns = new HashSet<string>(StringComparer.Ordinal) { AbsentGLN };
            foreach (IEvent evt in storedEvents.Events)
            {
                if (evt.Location?.GLN != null)
                {
                    glns.Add(evt.Location.GLN.ToString());
                }
                foreach (string? value in evt.SourceList.Select(s => s.Value).Concat(evt.DestinationList.Select(d => d.Value)))
                {
                    if (!string.IsNullOrWhiteSpace(value) && Uri.IsWellFormedUriString(value, UriKind.Absolute))
                    {
                        glns.Add(value);
                    }
                }
            }
            return glns.OrderBy(g => g, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Builds an <c>EQ_bizLocation</c> query for one GLN.
        /// </summary>
        /// <param name="gln">The GLN to query.</param>
        /// <returns>The query parameters.</returns>
        public static EPCISQueryParameters BizLocationQuery(string gln)
        {
            return new EPCISQueryParameters { query = new EPCISQuery { EQ_bizLocation = new List<Uri> { new Uri(gln) } } };
        }

        /// <summary>
        /// Gets the event IDs OpenTraceability's own filter returns for an <c>EQ_bizLocation</c> query.
        /// </summary>
        /// <param name="storedEvents">The events in the cache.</param>
        /// <param name="gln">The GLN to query.</param>
        /// <returns>The expected event IDs.</returns>
        public static HashSet<string> GetExpectedEventIds(EPCISQueryDocument storedEvents, string gln)
        {
            return storedEvents.FilterEvents(BizLocationQuery(gln)).Select(e => e.EventID!.ToString()).ToHashSet();
        }

        /// <summary>
        /// Queries every candidate GLN and describes each one whose results differ from the library's filter.
        /// </summary>
        /// <param name="service">The upgraded cache to query.</param>
        /// <param name="storedEvents">The events in the cache.</param>
        /// <returns>One description per mismatching GLN; empty when the cache agrees with the library everywhere.</returns>
        public static async Task<List<string>> FindBizLocationMismatchesAsync(IDatabaseService service, EPCISQueryDocument storedEvents)
        {
            List<string> mismatches = new List<string>();
            foreach (string gln in GetCandidateGLNs(storedEvents))
            {
                HashSet<string> expected = GetExpectedEventIds(storedEvents, gln);
                EPCISQueryDocument result = await service.QueryEvents(BizLocationQuery(gln));
                HashSet<string> actual = result.Events.Select(e => e.EventID!.ToString()).ToHashSet();
                if (!actual.SetEquals(expected))
                {
                    mismatches.Add($"{gln}: expected [{string.Join(", ", expected.Order())}] but got [{string.Join(", ", actual.Order())}]");
                }
            }
            return mismatches;
        }

        /// <summary>
        /// Counts the GLNs for which the legacy broad matching (any stored location GLN) disagrees with the library's filter.
        /// </summary>
        /// <remarks>
        /// Used as a precondition: a snapshot where this count is zero could not detect the bug, so the upgrade tests would pass vacuously.
        /// </remarks>
        /// <param name="storedEvents">The events in the cache.</param>
        /// <param name="legacyLocationGLNsByEventId">The location GLNs the legacy code indexed for each event.</param>
        /// <returns>The number of GLNs whose broad and strict results differ.</returns>
        public static int CountGLNsWhereLegacyMatchingDiffers(EPCISQueryDocument storedEvents, Dictionary<string, List<string>> legacyLocationGLNsByEventId)
        {
            int count = 0;
            foreach (string gln in GetCandidateGLNs(storedEvents))
            {
                string lowered = gln.ToLower();
                HashSet<string> broad = legacyLocationGLNsByEventId.Where(kvp => kvp.Value.Contains(lowered)).Select(kvp => kvp.Key).ToHashSet();
                if (!broad.SetEquals(GetExpectedEventIds(storedEvents, gln)))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Builds queries over every filter other than location, so results can be compared before and after the upgrade.
        /// </summary>
        /// <param name="storedEvents">The events in the cache.</param>
        /// <returns>Named queries: one per business step and action, a handful of EPCs, and the full event-time window.</returns>
        public static Dictionary<string, EPCISQueryParameters> BuildNonLocationQueries(EPCISQueryDocument storedEvents)
        {
            Dictionary<string, EPCISQueryParameters> queries = new Dictionary<string, EPCISQueryParameters>();

            foreach (string bizStep in storedEvents.Events.Where(e => e.BusinessStep != null).Select(e => e.BusinessStep!.ToString()).Distinct())
            {
                queries[$"bizStep:{bizStep}"] = new EPCISQueryParameters { query = new EPCISQuery { EQ_bizStep = new List<string> { bizStep } } };
            }

            foreach (string action in storedEvents.Events.Where(e => e.Action != null).Select(e => e.Action.ToString()!.ToLower()).Distinct())
            {
                queries[$"action:{action}"] = new EPCISQueryParameters { query = new EPCISQuery { EQ_action = new List<string> { action } } };
            }

            foreach (string epc in storedEvents.Events.SelectMany(e => e.Products).Select(p => p.EPC.ToString()).Distinct().Take(5))
            {
                queries[$"epc:{epc}"] = new EPCISQueryParameters { query = new EPCISQuery { MATCH_anyEPC = new List<string> { epc } } };
            }

            DateTimeOffset? first = storedEvents.Events.Min(e => e.EventTime);
            DateTimeOffset? last = storedEvents.Events.Max(e => e.EventTime);
            queries["eventTime:all"] = new EPCISQueryParameters { query = new EPCISQuery { GE_eventTime = first, LE_eventTime = last } };

            return queries;
        }

        /// <summary>
        /// Runs each named query and records the sorted event IDs it returns.
        /// </summary>
        /// <param name="service">The cache to query.</param>
        /// <param name="queries">The named queries to run.</param>
        /// <returns>The sorted event IDs per query name.</returns>
        public static async Task<Dictionary<string, List<string>>> CaptureResultsAsync(IDatabaseService service, Dictionary<string, EPCISQueryParameters> queries)
        {
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, EPCISQueryParameters> query in queries)
            {
                EPCISQueryDocument result = await service.QueryEvents(query.Value);
                results[query.Key] = result.Events.Select(e => e.EventID!.ToString()).OrderBy(id => id, StringComparer.Ordinal).ToList();
            }
            return results;
        }
    }
}
