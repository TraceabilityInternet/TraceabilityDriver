using TraceabilityDriver.Services.Connectors;

namespace TraceabilityDriver.Models.Mapping;

public class TDMapping
{
    /// <summary>
    /// The event type to map.
    /// </summary>
    public string EventType { get; set; } = string.Empty;   

    /// <summary>
    /// The selectors to use for the mapping.
    /// </summary>
    public List<TDMappingSelector> Selectors { get; set; } = new ();

    /// <summary>
    /// The fields to use for aligning events across multiple selectors.
    /// </summary>
    public List<string> IdentityFields { get; set; } = new ();
}