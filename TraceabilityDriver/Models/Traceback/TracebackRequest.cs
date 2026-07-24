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
        /// The digital link resolver URL of the external traceability server. Overrides the configured Traceback:ResolverUrl when supplied.
        /// </summary>
        public string? ResolverUrl { get; set; }

        /// <summary>
        /// The API key to send to the external traceability server. Overrides the configured Traceback:APIKey when supplied.
        /// </summary>
        public string? ApiKey { get; set; }
    }
}
