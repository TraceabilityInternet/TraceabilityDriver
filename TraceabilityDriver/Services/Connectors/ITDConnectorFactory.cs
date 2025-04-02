using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services.Connectors;

namespace TraceabilityDriver.Services.Connectors;

/// <summary>
/// The factory for creating connectors.
/// </summary>
public interface ITDConnectorFactory
{
    /// <summary>
    /// Creates a connector for the mapping configuration.
    /// </summary>
    ITDConnector CreateConnector(TDConnectorConfiguration configuration);
}

