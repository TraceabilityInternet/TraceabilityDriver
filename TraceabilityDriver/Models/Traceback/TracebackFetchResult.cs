using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Models.Traceback
{
    /// <summary>
    /// The outcome of executing a traceback against an external traceability server, before anything is persisted.
    /// </summary>
    public class TracebackFetchResult
    {
        /// <summary>
        /// The merged document containing all events and master data discovered by the traceback, deduplicated by event id.
        /// </summary>
        public EPCISDocument Document { get; set; } = new EPCISDocument();

        /// <summary>
        /// Errors encountered while tracing individual EPCs or resolving master data. A partial result is still usable.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
}
