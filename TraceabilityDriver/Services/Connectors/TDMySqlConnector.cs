using MySql.Data.MySqlClient;
using System.Data;
using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services.Connectors
{
    public class TDMySqlConnector : ITDConnector
    {
        private readonly ILogger<TDMySqlConnector> _logger;
        private readonly ISynchronizationContext _syncContext;
        private readonly IEventsTableMappingService _eventsTableMappingService;

        public TDMySqlConnector(
            ILogger<TDMySqlConnector> logger,
            IEventsTableMappingService eventsTableMappingService,
            ISynchronizationContext syncContext)
        {
            _logger = logger;
            _eventsTableMappingService = eventsTableMappingService;
            _syncContext = syncContext;
        }

        /// <summary>
        /// Tests the connection to the database.
        /// </summary>
        public async Task<bool> TestConnectionAsync(TDConnectorConfiguration config)
        {
            try
            {
                using (var connection = new MySqlConnection(config.ConnectionString))
                {
                    await connection.OpenAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error testing the connection to the database: {config.Database}", ex);
            }
        }

        /// <summary>
        /// Retrieves the total number of rows from a database asynchronously.
        /// </summary>
        /// <param name="config">Contains the configuration settings for connecting to the database.</param>
        /// <param name="selector">Specifies the query to count the rows in the database.</param>
        /// <returns>Returns the total number of rows as an integer.</returns>
        /// <exception cref="Exception">Thrown when there is an error while accessing the database.</exception>
        public async Task<int> GetTotalRowsAsync(TDConnectorConfiguration config, TDMappingSelector selector)
        {
            try
            {
                using (var connection = new MySqlConnection(config.ConnectionString))
                {
                    await connection.OpenAsync();

                    using (MySqlCommand cmd = new MySqlCommand(selector.Count, connection))
                    {
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting total rows from the database: {config.Database}", ex);
            }
        }

        /// <summary>
        /// Returns one or more events from the database using the selector.
        /// </summary>
        public async Task<IEnumerable<CommonEvent>> GetEventsAsync(TDConnectorConfiguration config, TDMappingSelector selector, CancellationToken cancellationToken)
        {
            try
            {
                // Check for cancellation.
                if (cancellationToken.IsCancellationRequested) return new List<CommonEvent>();

                // Get the total number of rows.
                int totalRows = await GetTotalRowsAsync(config, selector);
                totalRows = Math.Min(totalRows, 10000);

                // Update the sync history.
                _syncContext.CurrentSync.TotalItems = totalRows;
                _syncContext.CurrentSync.ItemsProcessed = 0;
                _syncContext.Updated();

                using (var connection = new MySqlConnection(config.ConnectionString))
                {
                    await connection.OpenAsync();

                    // Check for cancellation.
                    if (cancellationToken.IsCancellationRequested) return new List<CommonEvent>();

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter())
                    {
                        adapter.SelectCommand = new MySqlCommand(selector.Selector, connection);
                        adapter.SelectCommand.Parameters.Add("@offset", MySqlDbType.Int32).Value = 0;
                        adapter.SelectCommand.Parameters.Add("@limit", MySqlDbType.Int32).Value = 1000;

                        // Add a memory variable.
                        foreach (var memory in selector.Memory)
                        {
                            AddMemoryVariable(adapter.SelectCommand, memory.Key, memory.Value);
                        }

                        // Check for cancellation.
                        if (cancellationToken.IsCancellationRequested) return new List<CommonEvent>();

                        // Create a list to store the events.
                        List<CommonEvent> events = new List<CommonEvent>();

                        // We are going to page the data in chunks of 1000.
                        for (int start = 0; start < totalRows && start < 10000; start += 1000)
                        {
                            // Check for cancellation.
                            if (cancellationToken.IsCancellationRequested) return new List<CommonEvent>();

                            // Set the offset.
                            adapter.SelectCommand.Parameters["@offset"].Value = start;

                            // Get the data table.
                            DataTable dataTable = await FillWithRetryAsync(adapter, cancellationToken);

                            // Check for cancellation.
                            if (cancellationToken.IsCancellationRequested) return new List<CommonEvent>();

                            // Map the events.
                            List<CommonEvent> results = _eventsTableMappingService.MapEvents(selector.EventMapping, dataTable, cancellationToken);
                            events.AddRange(results);

                            // Handle the memory variables.
                            foreach (var memory in selector.Memory)
                            {
                                HandleMemoryVariables(dataTable, memory.Key, memory.Value);
                            }

                            // Update the sync.
                            _syncContext.CurrentSync.ItemsProcessed += results.Count;
                            _syncContext.Updated();
                        }

                        return events;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting events from the database: {config.Database}", ex);
            }
        }

        /// <summary>
        /// This is a method that will help to remember things about the current sync so that we can 
        /// use them in the next sync.
        /// </summary>
        public void HandleMemoryVariables(DataTable dt, string key, TDMappingSelectorMemoryVariable memory)
        {
            string column = memory.Field.Substring(1) ?? string.Empty;
            if (!dt.Columns.Contains(column))
            {
                throw new Exception($"The column {column} does not exist in the data table. Failed to process the memory variable {key}.");
            }

            List<object> values = new List<object>();
            foreach (DataRow row in dt.Rows)
            {
                values.Add(row[column]);
            }

            _syncContext.CurrentSync.Memory[key] = values.Last().ToString() ?? "";
        }

        public void AddMemoryVariable(MySqlCommand selectCommand, string key, TDMappingSelectorMemoryVariable memory)
        {
            try
            {
                string value = memory.DefaultValue;

                // If the previous sync has a value for the memory variable, then use that.
                if (_syncContext.PreviousSync?.Memory.ContainsKey(key) == true)
                {
                    value = _syncContext.PreviousSync.Memory[key];
                }

                // Do a switch on the data type and parse the value.
                switch (memory.DataType.ToLower())
                {
                    case "string": selectCommand.Parameters.Add($"@{key}", MySqlDbType.VarChar).Value = value; break;
                    case "int32": selectCommand.Parameters.Add($"@{key}", MySqlDbType.Int32).Value = int.Parse(value); break;
                    case "int64": selectCommand.Parameters.Add($"@{key}", MySqlDbType.Int64).Value = long.Parse(value); break;
                    case "double": selectCommand.Parameters.Add($"@{key}", MySqlDbType.Double).Value = double.Parse(value); break;
                    case "datetime": selectCommand.Parameters.Add($"@{key}", MySqlDbType.DateTime).Value = DateTime.Parse(value); break;
                    case "bool": selectCommand.Parameters.Add($"@{key}", MySqlDbType.Bit).Value = bool.Parse(value); break;
                    default: throw new Exception($"Unknown data type on memory variable: {memory.DataType}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding memory variable: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Fills a DataTable with data from a database, implementing retry logic for transient errors.
        /// </summary>
        /// <param name="adapter">Used to execute the fill operation on the DataTable.</param>
        /// <param name="cancellationToken">Allows the operation to be cancelled if requested.</param>
        /// <returns>Returns the filled DataTable after successful retrieval.</returns>
        private async Task<DataTable> FillWithRetryAsync(MySqlDataAdapter adapter, CancellationToken cancellationToken)
        {
            DataTable dataTable = new DataTable();

            // Add retry logic for Fill operation with exponential backoff
            int maxRetries = 3;
            int retryCount = 0;
            int delayMilliseconds = 1000; // Start with 1 second delay

            while (true)
            {
                try
                {
                    adapter.Fill(dataTable);
                    break; // Success, exit the retry loop
                }
                catch (MySqlException ex) when (ex.Number == -2 || ex.Number == 11 || ex.Message.Contains("timeout") && retryCount < maxRetries)
                {
                    // -2: Timeout, 11: General network error
                    retryCount++;

                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Failed to fill data table after {RetryCount} attempts", retryCount);
                        throw; // Re-throw after max retries
                    }

                    _logger.LogWarning("Database operation timed out, retrying ({RetryCount}/{MaxRetries}) after {Delay}ms. Error: {ErrorMessage}",
                        retryCount, maxRetries, delayMilliseconds, ex.Message);

                    // Check cancellation before waiting
                    if (cancellationToken.IsCancellationRequested) return new DataTable();

                    // Wait with exponential backoff
                    await Task.Delay(delayMilliseconds, cancellationToken);
                    delayMilliseconds *= 2; // Exponential backoff
                }
            }

            return dataTable;
        }
    }
}