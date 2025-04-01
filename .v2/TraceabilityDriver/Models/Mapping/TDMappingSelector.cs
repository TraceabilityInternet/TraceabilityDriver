using Newtonsoft.Json;
using TraceabilityDriver.Services.Connectors;

namespace TraceabilityDriver.Models.Mapping;

public class TDMappingSelector
{
    /// <summary>
    /// The database to select from.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// The SQL SELECT statement to execute.
    /// </summary>
    public string Selector { get; set; } = string.Empty;

    /// <summary>
    /// The SQL SELECT statement to return the count that will be returned from the rows.
    /// </summary>
    public string Count { get; set; } = string.Empty;

    /// <summary>
    /// The configuration for how variables can be remembered from one sync to the next.
    /// </summary>
    public Dictionary<string, TDMappingSelectorMemoryVariable> Memory { get; set; } = new();   

    /// <summary>
    /// The event mapping to use for the selector.
    /// </summary>
    [JsonConverter(typeof(TDEventMappingConverter))]
    public TDEventMapping EventMapping { get; set; } = new ();
}