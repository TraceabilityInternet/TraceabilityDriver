using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.Mapping;

namespace TraceabilityDriver.Services;

public delegate void OnSynchronizeStatusChanged(SyncHistoryItem syncHistoryItem);

public class SynchronizeService : ISynchronizeService
{
    private readonly ILogger<SynchronizeService> _logger;
    private readonly ITDConnectorFactory _connectorFactory;
    private readonly IEventsMergerService _eventsMergerService;
    private readonly IEventsConverterService _eventsConverterService;
    private readonly IMappingSource _mappingSource;
    private readonly IMongoDBService _mongoDBService;
    private readonly IMappingContext _mappingContext;

    private SyncHistoryItem? _currentSync = null;

    public SynchronizeService(
        ILogger<SynchronizeService> logger,
        ITDConnectorFactory connectorFactory,
        IEventsMergerService eventsMergerService,
        IEventsConverterService eventsConverterService,
        IMongoDBService mongoDBService,
        IMappingSource mappingSource,
        IMappingContext mappingContext)
    {
        _logger = logger;
        _connectorFactory = connectorFactory;
        _eventsMergerService = eventsMergerService;
        _eventsConverterService = eventsConverterService;
        _mongoDBService = mongoDBService;
        _logger.LogDebug("SynchronizeService initialized");
        _mappingSource = mappingSource;
        _mappingContext = mappingContext;
    }

    public event OnSynchronizeStatusChanged? OnSynchronizeStatusChanged;

    public SyncHistoryItem? CurrentSync { get => _currentSync; }

    /// <summary>
    /// Starts a synchronization process that checks if it needs to run based on the last execution time.
    /// </summary>
    /// <param name="cancellationToken">Used to signal when the synchronization process should be canceled.</param>
    /// <returns>Returns a completed task indicating the start of the synchronization process.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Execute the synchronization process at midnight every night or if it has not processed in the last 24 hours
        // then execute it immediately.
        _logger.LogInformation("SynchronizeService started");
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting synchronization process");
                    await SynchronizeAsync(cancellationToken);
                    _logger.LogInformation("Synchronization process completed");
                }
                catch (Exception ex)
                {
                    if (_currentSync != null)
                    {
                        _currentSync.Status = $"Error: {ex.Message}";
                    }
                    _logger.LogError(ex, "Error occurred during synchronization process");
                }
                finally
                {
                    if (_currentSync != null)
                    {
                        _currentSync.EndTime = DateTime.UtcNow;
                        OnSynchronizeStatusChanged?.Invoke(_currentSync);
                    }
                }

#if DEBUG
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
#else
                await Task.Delay(TimeSpan.FromMinutes(60), cancellationToken);
#endif
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the asynchronous operation.
    /// </summary>
    /// <param name="cancellationToken">Used to signal the cancellation of the operation.</param>
    /// <returns>Returns a completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Synchronizes the data from the database to the event store.
    /// </summary>
    public async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Synchronizing data from the database(s) to the event store.");
        _logger.LogDebug("Starting synchronization process");

        // Create the sync history item.
        _currentSync = new SyncHistoryItem();
        
        await UpdateSyncStatus(sync => sync.Status = "Starting sync...");

        /// Read the mapping files.
        foreach (TDMappingConfiguration mapping in _mappingSource.GetMappings())
        {
            // Set the current configuration being processed.
            _mappingContext.Configuration = mapping;

            /// Update the sync status.
            await UpdateSyncStatus(sync => sync.Status = "Processing mapping file...");

            try
            {
                /// Test the connections.
                _logger.LogDebug("Testing connections for mapping file.");
                if (!await TestConnectionsAsync(mapping))
                {
                    _logger.LogError("Failed to test the connections for the mapping file.");
                    continue;
                }
                _logger.LogDebug("All connections tested successfully for mapping file.");

                /// Verify the mappings are valid.
                _logger.LogDebug("Verifying mapping configuration.");
                if (!VerifyMapping(mapping))
                {
                    _logger.LogError("The mapping file: {Mapping} is invalid.", mapping);
                    continue;
                }
                _logger.LogDebug("Mapping verification successful for.");

                /// Process the mappings.
                _logger.LogDebug("Beginning to process mappings for file.");
                await ProcessMappingsAsync(mapping, cancellationToken);

                _logger.LogInformation("Successfully processed the mapping file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing the mapping file.");
            }
        }

        // Update the sync history item.
        await UpdateSyncStatus(sync =>
        {
            sync.Status = "Completed";
            sync.EndTime = DateTime.UtcNow;
        });

        _logger.LogDebug("Synchronization process completed");
    }

    /// <summary>
    /// Processes mappings defined in a configuration, handling events and storing them in a database.
    /// </summary>
    /// <param name="mappingFile">Specifies the file containing the mapping configurations to be processed.</param>
    /// <param name="mapping">Contains the configuration details for the mappings, including selectors and connections.</param>
    /// <returns>This method does not return a value.</returns>
    public async Task ProcessMappingsAsync(TDMappingConfiguration mapping, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing {Count} mappings from file.", mapping.Mappings.Count);

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

                    /// Get the number of records that will be processed by this selection.
                    _logger.LogDebug("Retrieving total rows for database: {Database}", selector.Database);
                    var totalRows = await connector.GetTotalRowsAsync(mapping.Connections[selector.Database], selector);
                    _logger.LogDebug("Retrieved {Count} total rows from database: {Database}", totalRows, selector.Database);

                    /// Update the sync status.
                    await UpdateSyncStatus(sync =>
                    {
                        sync.TotalItems = totalRows;
                        sync.ItemsProcessed = 0;
                    });

                    /// Get the events.
                    _logger.LogDebug("Retrieving events using selector for database: {Database}", selector.Database);
                    var retrievedEvents = await connector.GetEventsAsync(mapping.Connections[selector.Database], selector, cancellationToken, UpdateSyncStatus);
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

                _logger.LogInformation("Successfully processed a mapping for the event type '{EventType}' in the mapping file.", map.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing a mapping for the event type '{EventType}' in the mapping file.", map.EventType);
            }
        }

        _logger.LogDebug("Completed processing all mappings from file.");
    }

    /// <summary>
    /// Verifies that the mapping is valid.
    /// </summary>
    /// <param name="mappingFile">The mapping configuration file.</param>
    /// <param name="mapping">The mapping file path.</param>
    /// <returns></returns>
    public bool VerifyMapping(TDMappingConfiguration mapping)
    {
        _logger.LogDebug("Verifying mapping for file.");

        foreach (var map in mapping.Mappings)
        {
            _logger.LogDebug("Verifying mapping for event type: {EventType}", map.EventType);

            foreach (var selector in map.Selectors)
            {
                _logger.LogDebug("Checking if database '{Database}' is defined in connections", selector.Database);

                if (!mapping.Connections.ContainsKey(selector.Database))
                {
                    _logger.LogError("The database '{Database}' is not defined in the connections.", selector.Database);
                    return false;
                }

                _logger.LogDebug("Database '{Database}' found in connections", selector.Database);
            }

            _logger.LogDebug("All selectors for event type '{EventType}' have valid database references", map.EventType);
        }

        _logger.LogDebug("Mapping verification completed successfully for file.");
        return true;
    }

    /// <summary>
    /// Tests the validity of database connections defined in the provided mapping configuration.
    /// </summary>
    /// <param name="mappingFile">Specifies the file that contains the mapping configuration for the connections.</param>
    /// <param name="mapping">Contains the configuration details for the connections to be tested.</param>
    /// <returns>Indicates whether all tested connections are valid.</returns>
    public async Task<bool> TestConnectionsAsync(TDMappingConfiguration mapping)
    {
        _logger.LogDebug("Testing {Count} connections for file.", mapping.Connections.Count);

        // Test each connection.
        bool allConnectionsValid = true;
        foreach (var connection in mapping.Connections)
        {
            _logger.LogDebug("Testing connection to database: {ConnectionName}", connection.Key);

            var connector = _connectorFactory.CreateConnector(connection.Value);
            _logger.LogDebug("Created connector for database: {ConnectionName}, testing connection...", connection.Key);

            if (!await connector.TestConnectionAsync(connection.Value))
            {
                _logger.LogError("Failed to test the connection to the database: {ConnectionName}", connection.Key);
                allConnectionsValid = false;
                continue;
            }

            _logger.LogDebug("Successfully tested connection to database: {ConnectionName}", connection.Key);
        }

        _logger.LogDebug("Connection testing completed for mapping, all connections valid: {AllValid}",
            allConnectionsValid);

        return allConnectionsValid;
    }

    /// <summary>
    /// Updates the synchronization status with the provided action if a current sync is in progress.
    /// </summary>
    /// <param name="action">Executes a specified operation on the current synchronization item.</param>
    /// <returns>Completes the asynchronous operation without returning a value.</returns>
    public async Task UpdateSyncStatus(Action<SyncHistoryItem> action)
    {
        if (_currentSync != null)
        {
            action(_currentSync);
            OnSynchronizeStatusChanged?.Invoke(_currentSync);
        }

        await Task.CompletedTask;
    }
}
