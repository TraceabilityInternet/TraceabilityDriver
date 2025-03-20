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
    /// The field to use for the audit timestamp.
    /// </summary>
    public string AuditField { get; set; } = string.Empty;

    /// <summary>
    /// The event mapping to use for the selector.
    /// </summary>
    public TDEventMapping EventMapping { get; set; } = new ();
}