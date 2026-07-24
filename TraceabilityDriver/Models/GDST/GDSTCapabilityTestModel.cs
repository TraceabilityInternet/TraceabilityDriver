using Newtonsoft.Json;

namespace TraceabilityDriver.Models.GDST
{
    /// <summary>
    /// The start request/response model for the GDST capability tool's v2 API (POST v2/process/start).
    /// </summary>
    /// <remarks>
    /// The same model is used in both directions: the request carries the solution details and the
    /// top-of-chain EPCs of the solution's own test data (<see cref="SolutionProviderEPCs"/>); the
    /// echoed response is populated by the tool with the <see cref="ComplianceProcessUUID"/> and the
    /// tool-generated EPCs (<see cref="EpCs"/>) that the solution must trace back and ingest before
    /// advancing the test.
    /// </remarks>
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
        /// Gets or sets the tool-generated EPCs. Populated by the tool in the start response; the
        /// solution must trace these back from the tool and ingest the resulting data.
        /// </summary>
        [JsonProperty("epCs")]
        public List<string> EpCs { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the top-of-chain EPCs of the solution's own test data, which the tool traces
        /// back against the solution during the capability test. At least one is required.
        /// </summary>
        [JsonProperty("solutionProviderEPCs")]
        public List<string> SolutionProviderEPCs { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the GDST version under test. Must serialize as a JSON number; 20 = GDST 2.0,
        /// the only version the v2 API accepts.
        /// </summary>
        [JsonProperty("gdstVersion")]
        public int GdstVersion { get; set; } = 20;

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
