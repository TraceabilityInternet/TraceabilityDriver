using Newtonsoft.Json;

namespace TraceabilityDriver.Models.GDST
{
    public enum GDSTCapabilityTestStatus
    {
        Started = 0,
        Failed = 10,
        Passed = 11,
        Approved = 12,
        TimedOut = 13,
        Rejected = 14,
    }

    public class GDSTCapabilityTestResults
    {
        [JsonProperty("solutionName")]
        public string SolutionName { get; set; } = string.Empty;

        [JsonProperty("complianceProcessUUID")]
        public string ComplianceProcessUUID { get; set; } = string.Empty;

        [JsonProperty("status")]
        public GDSTCapabilityTestStatus Status { get; set; }

        [JsonProperty("stage")]
        public int Stage { get; set; }

        [JsonProperty("errors")]
        public List<GDSTCapabilityTestsError> Errors { get; set; } = new List<GDSTCapabilityTestsError>();

        [JsonProperty("errorReport")]
        public string ErrorReport { get; set; } = string.Empty;
    }

    public enum GDSTCapabilityTestsErrorType
    {
        SolutionData = 0,
        SystemData = 1,
        CompletenessCheck = 2
    }

    public class GDSTCapabilityTestsError
    {
        public GDSTCapabilityTestsErrorType Type { get; set; }
        public string? EventName { get; set; }
        public string? EventID { get; set; }
        public string? Error { get; set; }

        public override string ToString()
        {
            return $"{Type} - {EventName} - {EventID} - {Error}";
        }
    }
}
