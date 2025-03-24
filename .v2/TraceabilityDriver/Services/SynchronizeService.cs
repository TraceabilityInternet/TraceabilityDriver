using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services.Connectors;

namespace TraceabilityDriver.Services;

public class SynchronizeService : ISynchronizeService
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
        _logger.LogDebug("SynchronizeService initialized");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Execute the synchronization process at midnight every night or if it has not processed in the last 24 hours
        // then execute it immediately.
        _logger.LogInformation("SynchronizeService started");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Synchronizes the data from the database to the event store.
    /// </summary>
    public async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        /// TODO: Implement the logic to synchronize the data from the database to the event store.
        _logger.LogInformation("Synchronizing data from the database(s) to the event store.");
        _logger.LogDebug("Starting synchronization process");

        /// Get the mapping files.
        var mappingPath = Path.Combine(AppContext.BaseDirectory, "Mappings");
        _logger.LogDebug("Looking for mapping files in: {MappingPath}", mappingPath);
        var mappingFiles = Directory.GetFiles(mappingPath, "*.json");
        _logger.LogDebug("Found {Count} mapping files", mappingFiles.Length);

        /// Read the mapping files.
        foreach (var mappingFile in mappingFiles)
        {
            _logger.LogInformation("Processing the mapping file: {MappingFile}", mappingFile);
            _logger.LogDebug("Beginning to parse mapping file: {MappingFile}", mappingFile);
            try
            {
                var jsonContent = File.ReadAllText(mappingFile);
                _logger.LogDebug("Successfully read mapping file content, length: {Length} characters", jsonContent.Length);

                var mapping = JsonConvert.DeserializeObject<TDMappingConfiguration>(jsonContent)
                    ?? throw new Exception("Failed to deserialize the mapping file.");

                _logger.LogDebug("Successfully deserialized mapping file with {ConnectionsCount} connections and {MappingsCount} mappings",
                    mapping.Connections.Count, mapping.Mappings.Count);

                /// Test the connections.
                _logger.LogDebug("Testing connections for mapping file: {MappingFile}", mappingFile);
                if (!await TestConnectionsAsync(mappingFile, mapping))
                {
                    _logger.LogError("Failed to test the connections for the mapping file: {MappingFile}", mappingFile);
                    continue;
                }
                _logger.LogDebug("All connections tested successfully for mapping file: {MappingFile}", mappingFile);

                /// Verify the mappings are valid.
                _logger.LogDebug("Verifying mapping configuration: {MappingFile}", mappingFile);
                if (!VerifyMapping(mappingFile, mapping))
                {
                    _logger.LogError("The mapping file: {MappingFile} is invalid.", mappingFile);
                    continue;
                }
                _logger.LogDebug("Mapping verification successful for: {MappingFile}", mappingFile);

                /// Process the mappings.
                _logger.LogDebug("Beginning to process mappings for file: {MappingFile}", mappingFile);
                await ProcessMappingsAsync(mappingFile, mapping, cancellationToken);

                _logger.LogInformation("Successfully processed the mapping file: {MappingFile}", mappingFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing the mapping file: {MappingFile}", mappingFile);
            }
        }

        _logger.LogDebug("Synchronization process completed");
    }

    /// <summary>
    /// Processes mappings defined in a configuration, handling events and storing them in a database.
    /// </summary>
    /// <param name="mappingFile">Specifies the file containing the mapping configurations to be processed.</param>
    /// <param name="mapping">Contains the configuration details for the mappings, including selectors and connections.</param>
    /// <returns>This method does not return a value.</returns>
    public async Task ProcessMappingsAsync(string mappingFile, TDMappingConfiguration mapping, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing {Count} mappings from file: {MappingFile}", mapping.Mappings.Count, mappingFile);

        foreach (var map in mapping.Mappings)
        {
            _logger.LogDebug("Processing mapping for event type: {EventType}", map.EventType);

            /// Check for cancellation.
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Cancellation requested, stopping processing of mappings");
                return;
            }

            try
            {
                List<CommonEvent> events = new List<CommonEvent>();
                _logger.LogDebug("Processing {Count} selectors for event type: {EventType}", map.Selectors.Count, map.EventType);

                foreach (var selector in map.Selectors)
                {
                    _logger.LogDebug("Processing selector for database: {Database}", selector.Database);

                    /// Check for cancellation.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogDebug("Cancellation requested during selector processing");
                        return;
                    }

                    /// Create the connector.
                    _logger.LogDebug("Creating connector for database: {Database}", selector.Database);
                    var connector = _connectorFactory.CreateConnector(mapping.Connections[selector.Database]);
                    _logger.LogDebug("Connector created successfully for database: {Database}", selector.Database);

                    /// Get the events.
                    _logger.LogDebug("Retrieving events using selector for database: {Database}", selector.Database);
                    var retrievedEvents = await connector.GetEventsAsync(selector, cancellationToken);
                    _logger.LogDebug("Retrieved {Count} events from database: {Database}", retrievedEvents.Count(), selector.Database);
                    events.AddRange(retrievedEvents);
                }

                /// Check for cancellation.
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Cancellation requested after retrieving all events");
                    return;
                }

                /// Merge the events.
                _logger.LogDebug("Merging {Count} events for event type: {EventType}", events.Count, map.EventType);
                var mergedEvents = await _eventsMergerService.MergeEventsAsync(map, events);
                _logger.LogDebug("Merged into {Count} events for event type: {EventType}", mergedEvents.Count, map.EventType);

                /// Check for cancellation.
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Cancellation requested after merging events");
                    return;
                }

                /// Convert the events.
                _logger.LogDebug("Converting {Count} merged events to EPCIS format", mergedEvents.Count);
                var epcisDoc = await _eventsConverterService.ConvertEventsAsync(mergedEvents);
                _logger.LogDebug("Conversion complete - generated {EventCount} EPCIS events and {MasterDataCount} master data elements",
                    epcisDoc.Events.Count, epcisDoc.MasterData.Count);

                /// Check for cancellation.
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Cancellation requested after converting events");
                    return;
                }

                /// Save the events.
                _logger.LogDebug("Storing {Count} events in MongoDB", epcisDoc.Events.Count);
                await _mongoDBService.StoreEventsAsync(epcisDoc.Events);
                _logger.LogDebug("Successfully stored {Count} events in MongoDB", epcisDoc.Events.Count);

                /// Save the master data.
                _logger.LogDebug("Storing {Count} master data elements in MongoDB", epcisDoc.MasterData.Count);
                await _mongoDBService.StoreMasterDataAsync(epcisDoc.MasterData);
                _logger.LogDebug("Successfully stored {Count} master data elements in MongoDB", epcisDoc.MasterData.Count);

                _logger.LogInformation("Successfully processed a mapping for the event type '{EventType}' in the mapping file: {MappingFile}", map.EventType, mappingFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing a mapping for the event type '{EventType}' in the mapping file: {MappingFile}", map.EventType, mappingFile);
            }
        }

        _logger.LogDebug("Completed processing all mappings from file: {MappingFile}", mappingFile);
    }

    /// <summary>
    /// Verifies that the mapping is valid.
    /// </summary>
    /// <param name="mappingFile">The mapping configuration file.</param>
    /// <param name="mapping">The mapping file path.</param>
    /// <returns></returns>
    public bool VerifyMapping(string mappingFile, TDMappingConfiguration mapping)
    {
        _logger.LogDebug("Verifying mapping for file: {MappingFile}", mappingFile);

        foreach (var map in mapping.Mappings)
        {
            _logger.LogDebug("Verifying mapping for event type: {EventType}", map.EventType);

            foreach (var selector in map.Selectors)
            {
                _logger.LogDebug("Checking if database '{Database}' is defined in connections", selector.Database);

                if (!mapping.Connections.ContainsKey(selector.Database))
                {
                    _logger.LogError("The database '{Database}' is not defined in the connections: {MappingFile}", selector.Database, mappingFile);
                    return false;
                }

                _logger.LogDebug("Database '{Database}' found in connections", selector.Database);
            }

            _logger.LogDebug("All selectors for event type '{EventType}' have valid database references", map.EventType);
        }

        _logger.LogDebug("Mapping verification completed successfully for file: {MappingFile}", mappingFile);
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
        _logger.LogDebug("Testing {Count} connections for file: {MappingFile}", mapping.Connections.Count, mappingFile);

        // Test each connection.
        bool allConnectionsValid = true;
        foreach (var connection in mapping.Connections)
        {
            _logger.LogDebug("Testing connection to database: {ConnectionName}", connection.Key);

            var connector = _connectorFactory.CreateConnector(connection.Value);
            _logger.LogDebug("Created connector for database: {ConnectionName}, testing connection...", connection.Key);

            if (!await connector.TestConnectionAsync())
            {
                _logger.LogError("Failed to test the connection to the database: {ConnectionName}", connection.Key);
                allConnectionsValid = false;
                continue;
            }

            _logger.LogDebug("Successfully tested connection to database: {ConnectionName}", connection.Key);
        }

        _logger.LogDebug("Connection testing completed for file: {MappingFile}, all connections valid: {AllValid}",
            mappingFile, allConnectionsValid);

        return allConnectionsValid;
    }
}
