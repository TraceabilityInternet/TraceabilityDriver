using TraceabilityDriver.Services.Connectors;

namespace TraceabilityDriver.Models.Mapping;

public class TDMappingConfiguration
{
    /// <summary>
    /// The mappings to use for the configuration.
    /// </summary>
    public List<TDMapping> Mappings { get; set; } = new ();

    /// <summary>
    /// Dictionaries to be used by the mappings.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> Dictionaries { get; set; } = new();

    /// <summary>
    /// The connections to use for the configuration.
    /// </summary>
    public Dictionary<string, TDConnectorConfiguration> Connections { get; set; } = new ();
}