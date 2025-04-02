namespace TraceabilityDriver.Services.Connectors;

/// <summary>
/// The type of the connector.
/// </summary>
public enum ConnectorType
{
    Unknown = 0,
    SqlServer = 1,
}

/// <summary>
/// The configuration for the connector.
/// </summary>
public class TDConnectorConfiguration
{
    /// <summary>
    /// The name of the database to use for the connector. This aligns with the database name in the 
    /// mapping file.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// The connection string for the connector.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The type of the connector.
    /// </summary>
    public ConnectorType ConnectorType { get; set; }
}

