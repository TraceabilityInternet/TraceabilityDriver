using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services.Connectors;

namespace TraceabilityDriver.Services;

public class SynchronizeService : IHostedService, ISynchronizeService
{
    private readonly ILogger<SynchronizeService> _logger;
    private readonly ITDConnectorFactory _connectorFactory;
    private readonly IEventsMergerService _eventsMergerService;
    private readonly IEventsConverterService _eventsConverterService;
    private readonly IMongoDBService _mongoDBService;

    public SynchronizeService(
        ILogger<SynchronizeService> logger,
        ITDConnectorFactory connectorFactory,
        IEventsMergerService eventsMergerService,
        IEventsConverterService eventsConverterService,
        IMongoDBService mongoDBService)
    {
        _logger = logger;
        _connectorFactory = connectorFactory;
        _eventsMergerService = eventsMergerService;
        _eventsConverterService = eventsConverterService;
        _mongoDBService = mongoDBService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start task that repeats until the cancellation token is called.
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await SynchronizeAsync(cancellationToken);
                await Task.Delay(60000, cancellationToken);
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Synchronizes the data from the database to the event store.
    /// </summary>
    public async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        /// TODO: Implement the logic to synchronize the data from the database to the event store.
        _logger.LogInformation("Synchronizing data from the database(s) to the event store.");

        /// Get the mapping files.
        var mappingFiles = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Mapping"), "*.json");

        /// Read the mapping files.
        foreach (var mappingFile in mappingFiles)
        {
            _logger.LogInformation("Processing the mapping file: {MappingFile}", mappingFile);
            try
            {
                var mapping = JsonConvert.DeserializeObject<TDMappingConfiguration>(File.ReadAllText(mappingFile))
                    ?? throw new Exception("Failed to deserialize the mapping file.");

                /// Test the connections.
                if (!await TestConnectionsAsync(mappingFile, mapping))
                {
                    _logger.LogError("Failed to test the connections for the mapping file: {MappingFile}", mappingFile);
                    continue;
                }

                /// Verify the mappings are valid.
                if (!VerifyMapping(mappingFile, mapping))
                {
                    _logger.LogError("The mapping file: {MappingFile} is invalid.", mappingFile);
                    continue;
                }

                /// Process the mappings.
                await ProcessMappingsAsync(mappingFile, mapping, cancellationToken);

                _logger.LogInformation("Successfully processed the mapping file: {MappingFile}", mappingFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing the mapping file: {MappingFile}", mappingFile);
            }
        }
    }

    /// <summary>
    /// Processes mappings defined in a configuration, handling events and storing them in a database.
    /// </summary>
    /// <param name="mappingFile">Specifies the file containing the mapping configurations to be processed.</param>
    /// <param name="mapping">Contains the configuration details for the mappings, including selectors and connections.</param>
    /// <returns>This method does not return a value.</returns>
    public async Task ProcessMappingsAsync(string mappingFile, TDMappingConfiguration mapping, CancellationToken cancellationToken)
    {
        foreach (var map in mapping.Mappings)
        {
            /// Check for cancellation.
            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                List<CommonEvent> events = new List<CommonEvent>();

                foreach (var selector in map.Selectors)
                {
                    /// Check for cancellation.
                    if (cancellationToken.IsCancellationRequested) return;

                    /// Create the connector.
                    var connector = _connectorFactory.CreateConnector(mapping.Connections[selector.Database]);

                    /// Get the events.
                    events.AddRange(await connector.GetEventsAsync(selector, cancellationToken));
                }

                /// Check for cancellation.
                if (cancellationToken.IsCancellationRequested) return;

                /// Merge the events.
                var mergedEvents = await _eventsMergerService.MergeEventsAsync(map, events);

                /// Check for cancellation.
                if (cancellationToken.IsCancellationRequested) return;

                /// Convert the events.
                var epcisDoc = await _eventsConverterService.ConvertEventsAsync(mergedEvents);

                /// Check for cancellation.
                if (cancellationToken.IsCancellationRequested) return;

                /// Save the events.
                await _mongoDBService.StoreEventsAsync(epcisDoc.Events);

                /// Save the master data.
                await _mongoDBService.StoreMasterDataAsync(epcisDoc.MasterData);

                _logger.LogInformation("Successfully processed a mapping for the event type '{EventType}' in the mapping file: {MappingFile}", map.EventType, mappingFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing a mapping for the event type '{EventType}' in the mapping file: {MappingFile}", map.EventType, mappingFile);
            }
        }
    }

    /// <summary>
    /// Verifies that the mapping is valid.
    /// </summary>
    /// <param name="mappingFile">The mapping configuration file.</param>
    /// <param name="mapping">The mapping file path.</param>
    /// <returns></returns>
    public bool VerifyMapping(string mappingFile, TDMappingConfiguration mapping)
    {
        foreach (var map in mapping.Mappings)
        {
            foreach (var selector in map.Selectors)
            {
                if (!mapping.Connections.ContainsKey(selector.Database))
                {
                    _logger.LogError("The database '{Database}' is not defined in the connections: {MappingFile}", selector.Database, mappingFile);
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Tests the validity of database connections defined in the provided mapping configuration.
    /// </summary>
    /// <param name="mappingFile">Specifies the file that contains the mapping configuration for the connections.</param>
    /// <param name="mapping">Contains the configuration details for the connections to be tested.</param>
    /// <returns>Indicates whether all tested connections are valid.</returns>
    public async Task<bool> TestConnectionsAsync(string mappingFile, TDMappingConfiguration mapping)
    {
        // Test each connection.
        bool allConnectionsValid = true;
        foreach (var connection in mapping.Connections)
        {
            var connector = _connectorFactory.CreateConnector(connection.Value);
            if (!await connector.TestConnectionAsync())
            {
                _logger.LogError("Failed to test the connection to the database: {ConnectionName}", connection.Key);
                allConnectionsValid = false;
                continue;
            }
        }

        return allConnectionsValid;
    }
}
