namespace TraceabilityDriver.Models.Traceback
{
    /// <summary>
    /// Deployment defaults for tracebacks, bound from the "Traceback" configuration section.
    /// </summary>
    /// <remarks>
    /// A request body may override <see cref="ResolverUrl"/> and <see cref="APIKey"/> per call; these settings
    /// exist so that a deployment pointed at a single external traceability server does not need to repeat the
    /// URL and key on every request.
    /// </remarks>
    public class TracebackSettings
    {
        /// <summary>
        /// The default digital link resolver URL of the external traceability server.
        /// </summary>
        public string? ResolverUrl { get; set; }

        /// <summary>
        /// The default API key sent to the external traceability server.
        /// </summary>
        public string? APIKey { get; set; }

        /// <summary>
        /// The GS1 Digital Link Resolver standard version of the external server: "1.2.0" (linkset, default) or "1.1.2" (legacy flat array).
        /// </summary>
        public string ResolverVersion { get; set; } = "1.2.0";
    }
}
