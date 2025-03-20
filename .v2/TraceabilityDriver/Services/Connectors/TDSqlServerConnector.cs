using TraceabilityDriver.Models.Mapping;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Options;

namespace TraceabilityDriver.Services.Connectors;

public class TDSqlServerConnector : ITDConnector
{
    private readonly TDConnectorConfiguration _configuration;
    private readonly ILogger<TDSqlServerConnector> _logger;
    private readonly IEventsTableMappingService _eventsTableMappingService;

    public TDSqlServerConnector(
        IOptions<TDConnectorConfiguration> configuration,
        ILogger<TDSqlServerConnector> logger,
        IEventsTableMappingService eventsTableMappingService)
    {
        _configuration = configuration.Value;
        _logger = logger;
        _eventsTableMappingService = eventsTableMappingService;
    }

    /// <summary>
    /// Tests the connection to the database.
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using (var connection = new SqlConnection(_configuration.ConnectionString))
            {
                await connection.OpenAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error testing the connection to the database: {_configuration.Database}", ex);
        }
    }

    /// <summary>
    /// Returns one or more events from the database using the selector.
    /// </summary>
    public async Task<IEnumerable<CommonEvent>> GetEventsAsync(TDMappingSelector selector, CancellationToken cancellationToken)
    {
        try
        {
            /// Check for cancellation.
            if (cancellationToken.IsCancellationRequested) return new List<CommonEvent>();

            using (var connection = new SqlConnection(_configuration.ConnectionString))
            {
                await connection.OpenAsync();

                /// Check for cancellation.
                if (cancellationToken.IsCancellationRequested) return new List<CommonEvent>();

                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {
                    adapter.SelectCommand = new SqlCommand(selector.Selector, connection);

                    /// Check for cancellation.
                    if (cancellationToken.IsCancellationRequested) return new List<CommonEvent>();

                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    /// Check for cancellation.
                    if (cancellationToken.IsCancellationRequested) return new List<CommonEvent>();

                    List<CommonEvent> events = _eventsTableMappingService.MapEvents(selector.EventMapping, dataTable, cancellationToken);

                    return events;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting events from the database: {_configuration.Database}", ex);
        }
    }
}

