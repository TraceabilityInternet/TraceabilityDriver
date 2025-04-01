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
}