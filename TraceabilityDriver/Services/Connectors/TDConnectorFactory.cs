using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services.Connectors;

/// <summary>
/// The factory for creating a connector.
/// </summary>
public class TDConnectorFactory : ITDConnectorFactory
{
    private readonly ILogger<TDConnectorFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TDConnectorFactory(ILogger<TDConnectorFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a connector.
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    /// <returns>The connector.</returns>
    public ITDConnector CreateConnector(TDConnectorConfiguration connectorConfiguration)
    {
        try 
        {
            switch (connectorConfiguration.ConnectorType)
            {
                case ConnectorType.SqlServer:
                    return _serviceProvider.GetRequiredService<TDSqlServerConnector>();
                default:
                    throw new Exception($"Unsupported connector type: {connectorConfiguration.ConnectorType}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating a connector for the connector configuration: {connectorConfiguration.Database}", ex);
        }
    }
}

