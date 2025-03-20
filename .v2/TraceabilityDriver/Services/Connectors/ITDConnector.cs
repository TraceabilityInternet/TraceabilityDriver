using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services.Connectors;

/// <summary>
/// The interface for the connector.
/// </summary>
public interface ITDConnector
{
    /// <summary>
    /// Returns one or more events from the database using the selector.
    /// </summary>
    Task<IEnumerable<CommonEvent>> GetEventsAsync(TDMappingSelector selector, CancellationToken cancelToken);

    /// <summary>
    /// Tests the connection to the database.
    /// </summary>
    Task<bool> TestConnectionAsync();
}

