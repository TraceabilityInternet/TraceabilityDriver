namespace TraceabilityDriver.Models.GDST
{
    /// <summary>
    /// Settings for the GDST capability testing tool.
    /// </summary>
    public class GDSTCapabilityTestSettings
    {
        /// <summary>
        /// Gets or sets the URL of the capability tool.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key for authentication with the capability tool.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the solution name.
        /// </summary>
        public string SolutionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Party Global Location Number (PGLN) in URN format.
        /// </summary>
        public string PGLN { get; set; } = string.Empty;
    }
}
