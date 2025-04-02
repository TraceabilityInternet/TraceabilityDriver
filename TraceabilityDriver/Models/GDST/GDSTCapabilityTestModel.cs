using Newtonsoft.Json;

namespace TraceabilityDriver.Models.GDST
{
    /// <summary>
    /// Represents a model for testing GDS capabilities. It serves as a data structure for related test information.
    /// </summary>
    public class GDSTCapabilityTestModel
    {
        /// <summary>
        /// Gets or sets the name of the solution.
        /// </summary>
        [JsonProperty("solutionName")]
        public string SolutionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version of the solution.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the compliance process UUID.
        /// </summary>
        [JsonProperty("complianceProcessUUID")]
        public string ComplianceProcessUUID { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of EPCs.
        /// </summary>
        [JsonProperty("epCs")]
        public List<string> EpCs { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the GDST version.
        /// </summary>
        [JsonProperty("gdstVersion")]
        public string GdstVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the PGLN (Party Global Location Number).
        /// </summary>
        [JsonProperty("pgln")]
        public string Pgln { get; set; } = string.Empty;
    }
}
