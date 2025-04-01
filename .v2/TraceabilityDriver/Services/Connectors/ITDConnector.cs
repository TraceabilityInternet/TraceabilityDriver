using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Models.MongoDB;

namespace TraceabilityDriver.Services.Connectors;

/// <summary>
/// The interface for the connector.
/// </summary>
public interface ITDConnector
{
    /// <summary>
    /// Returns one or more events from the database using the selector.
    /// </summary>
    Task<IEnumerable<CommonEvent>> GetEventsAsync(TDConnectorConfiguration config, TDMappingSelector selector, CancellationToken cancelToken, Func<Action<SyncHistoryItem>, Task>? update);

    /// <summary>
    /// Asynchronously retrieves the total number of rows based on the provided configuration and mapping selector.
    /// </summary>
    /// <param name="config">Specifies the configuration settings for the data connector.</param>
    /// <param name="selector">Defines the mapping criteria for selecting the relevant data.</param>
    /// <returns>Returns the total count of rows as an integer.</returns>
    Task<int> GetTotalRowsAsync(TDConnectorConfiguration config, TDMappingSelector selector);

    /// <summary>
    /// Tests the connection to the database.
    /// </summary>
    Task<bool> TestConnectionAsync(TDConnectorConfiguration config);
}

