namespace TraceabilityDriver.Models.Traceback
{
    /// <summary>
    /// The body of a POST /traceback request.
    /// </summary>
    public class TracebackRequest
    {
        /// <summary>
        /// The EPCs to trace back. At least one is required.
        /// </summary>
        public List<string> Epcs { get; set; } = new List<string>();

        /// <summary>
        /// The digital link resolver URL of the external traceability server. Required.
        /// </summary>
        public string? ResolverUrl { get; set; }

        /// <summary>
        /// The API key sent to the external traceability server when supplied. Optional.
        /// </summary>
        /// <remarks>
        /// When the traceback is queued, the request (including this key) is serialized into the
        /// job storage and remains there until the job expires.
        /// </remarks>
        public string? ApiKey { get; set; }

        /// <summary>
        /// When true, the traceback runs inline and the response contains the finalized record.
        /// When false (the default), the traceback is queued and the response contains the queued
        /// record whose id can be polled via GET /traceback/{id}.
        /// </summary>
        public bool Synchronous { get; set; } = false;
    }
}
