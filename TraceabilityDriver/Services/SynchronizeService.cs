using Extensions;
using OpenTraceability.Models.Events;
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
    private readonly IDatabaseService _dbService;
    private readonly ISynchronizationContext _syncContext;

    public SynchronizeService(
        ILogger<SynchronizeService> logger,
        ITDConnectorFactory connectorFactory,
        IEventsMergerService eventsMergerService,
        IEventsConverterService eventsConverterService,
        IDatabaseService dbService,
        IMappingSource mappingSource,
        ISynchronizationContext syncContext)
    {
        _logger = logger;
        _connectorFactory = connectorFactory;
        _eventsMergerService = eventsMergerService;
        _eventsConverterService = eventsConverterService;
        _dbService = dbService;
        _logger.LogDebug("SynchronizeService initialized");
        _mappingSource = mappingSource;
        _syncContext = syncContext;
    }

    /// <summary>
    /// Synchronizes the data from the database to the event store.
    /// </summary>
    public async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Synchronizing data from the database(s) to the event store.");

        _syncContext.Configuration = null;
        _syncContext.PreviousSync = null;
        _syncContext.CurrentSync = new SyncHistoryItem();
        _syncContext.CurrentSync.Message = "Loading mapping files...";
        _syncContext.Updated();

        try
        {
            // Load the previous sync item.
            _syncContext.PreviousSync = (await _dbService.GetLatestSyncs(1)).FirstOrDefault();

            // Read the mapping files.
            foreach (TDMappingConfiguration mapping in _mappingSource.GetMappings())
            {
                // Set the current configuration being processed.
                _syncContext.Configuration = mapping;

                // Test the connections.
                if (!await TestConnectionsAsync(mapping))
                {
                    _logger.LogError("Failed to test the connections for the mapping file.");
                    continue;
                }

                // Verify the mappings are valid.
                if (!VerifyMapping(mapping))
                {
                    _logger.LogError("The mapping file: {Mapping} is invalid.", mapping);
                    continue;
                }

                // Process the mappings.
                await ProcessMappingsAsync(mapping, cancellationToken);

                _logger.LogInformation("Successfully processed the mapping file.");
            }

            // Update the sync history item for success
            _syncContext.CurrentSync.Status = SyncStatus.Completed;
            _syncContext.CurrentSync.EndTime = DateTime.UtcNow;
            _syncContext.Updated();
        }
        catch (Exception ex)
        {
            // Update the sync history item for failure
            _syncContext.CurrentSync.Status = SyncStatus.Failed;
            _syncContext.CurrentSync.EndTime = DateTime.UtcNow;
            _syncContext.CurrentSync.Message = $"Synchronization failed: {ex.Message}";
            _syncContext.Updated();

            _logger.LogError(ex, "Synchronization process failed");
            throw;
        }
        finally
        {
            // Store the sync history item in the database regardless of success or failure
            await _dbService.StoreSyncHistory(_syncContext.CurrentSync);
            _logger.LogInformation("Sync history saved to database");

            _logger.LogDebug("Synchronization process completed");
        }
    }

    /// <param name="mapping">Contains the configuration details for the mappings, including selectors and connections.</param>
    /// <returns>This method does not return a value.</returns>
    public async Task ProcessMappingsAsync(TDMappingConfiguration mapping, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing {Count} mappings from file.", mapping.Mappings.Count);

        foreach (var map in mapping.Mappings)
        {
            _logger.LogInformation("Processing mapping for event type: {EventType}", map.EventType);

            /// Check for cancellation.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                List<CommonEvent> events = new List<CommonEvent>();

                foreach (var selector in map.Selectors)
                {
                    // Check for cancellation.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    // Create the connector.
                    var connector = _connectorFactory.CreateConnector(mapping.Connections[selector.Database]);

                    // Get the number of records that will be processed by this selection.
                    var totalRows = await connector.GetTotalRowsAsync(mapping.Connections[selector.Database], selector);

                    // Update the sync status.
                    _syncContext.CurrentSync.TotalItems = totalRows;
                    _syncContext.CurrentSync.ItemsProcessed = 0;
                    _syncContext.CurrentSync.Message = "Reading events from database...";
                    _syncContext.Updated();

                    // Get the events.
                    var retrievedEvents = await connector.GetEventsAsync(mapping.Connections[selector.Database], selector, cancellationToken);
                    _logger.LogInformation("Retrieved {Count} events from database: {Database}", retrievedEvents.Count(), selector.Database);
                    events.AddRange(retrievedEvents);
                }

                // Check for cancellation.
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _syncContext.CurrentSync.Message = $"Merging events...";
                _syncContext.CurrentSync.ItemsProcessed = Convert.ToInt32((double)_syncContext.CurrentSync.TotalItems * 0.8);
                _syncContext.Updated();

                // Merge the events.
                var mergedEvents = await _eventsMergerService.MergeEventsAsync(map, events);
                _logger.LogInformation("Merged into {Count} events for event type: {EventType}", mergedEvents.Count, map.EventType);

                // Check for cancellation.
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Convert the events.
                _syncContext.CurrentSync.Message = $"Converting events...";
                _syncContext.CurrentSync.TotalItems = mergedEvents.Count;
                _syncContext.CurrentSync.ItemsProcessed = 0;
                _syncContext.Updated();

                EPCISDocument doc = new EPCISDocument();
                foreach (var batch in mergedEvents.Batch(100))
                {
                    var epcisDoc = await _eventsConverterService.ConvertEventsAsync(batch);
                    doc.Merge(epcisDoc);

                    _syncContext.CurrentSync.ItemsProcessed += batch.Count();
                    _syncContext.Updated();
                }

                // Check for cancellation.
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Save the events.
                _syncContext.CurrentSync.Message = $"Saving events...";
                _syncContext.CurrentSync.TotalItems = doc.Events.Count;
                _syncContext.CurrentSync.ItemsProcessed = 0;
                _syncContext.Updated();

                foreach (var batch in doc.Events.Batch(100))
                {
                    await _dbService.StoreEventsAsync(batch);

                    _syncContext.CurrentSync.ItemsProcessed += batch.Count();
                    _syncContext.Updated();
                }

                // Save the master data.
                _syncContext.CurrentSync.Message = $"Saving master data...";
                _syncContext.CurrentSync.TotalItems = doc.MasterData.Count;
                _syncContext.CurrentSync.ItemsProcessed = 0;
                _syncContext.Updated();

                foreach (var batch in doc.MasterData.Batch(100))
                {
                    await _dbService.StoreMasterDataAsync(batch);

                    _syncContext.CurrentSync.ItemsProcessed += batch.Count();
                    _syncContext.Updated();
                }

                _logger.LogInformation("Successfully processed a mapping for the event type '{EventType}' in the mapping file.", map.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing a mapping for the event type '{EventType}' in the mapping file.", map.EventType);
                throw;
            }
        }

        _logger.LogInformation("Completed processing all mappings from file.");
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
}
