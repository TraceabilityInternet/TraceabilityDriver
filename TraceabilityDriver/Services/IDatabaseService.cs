using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Interface for the GDST data cache, where the transformed traceability data is stored.
    /// </summary>
    public interface IDatabaseService
    {
        Task<DatabaseReport> GetDatabaseReport();
        Task<List<LogModel>> GetLastErrors(int top = 10);
        Task<List<SyncHistoryItem>> GetLatestSyncs(int top = 10);
        Task InitializeDatabase();
        Task<EPCISQueryDocument> QueryEvents(EPCISQueryParameters options);
        Task<IVocabularyElement?> QueryMasterData(string identifier);

        /// <summary>
        /// Upserts the events into the cache by event id and reports which event ids were inserted versus updated.
        /// </summary>
        Task<DatabaseStoreResult> StoreEventsAsync(List<IEvent> events);

        /// <summary>
        /// Upserts the master data into the cache by element id and reports which element ids were inserted versus updated.
        /// </summary>
        Task<DatabaseStoreResult> StoreMasterDataAsync(List<IVocabularyElement> masterData);

        Task StoreSyncHistory(SyncHistoryItem syncHistory);

        /// <summary>
        /// Upserts the traceback record by its id. Called once when a run starts and again when it finishes.
        /// </summary>
        Task StoreTracebackAsync(TracebackRecord traceback);

        /// <summary>
        /// Upserts the ledger entries by their (TracebackId, ItemType, ItemId) key, so retried writes never duplicate entries.
        /// </summary>
        Task StoreTracebackItemsAsync(List<TracebackItem> items);

        /// <summary>
        /// Returns traceback records ordered by start time descending.
        /// </summary>
        Task<List<TracebackRecord>> GetTracebacksAsync(int top = 100, int skip = 0);

        /// <summary>
        /// Returns the traceback record with the given id, or null when it does not exist.
        /// </summary>
        Task<TracebackRecord?> GetTracebackAsync(string id);

        /// <summary>
        /// Returns the ledger entries recorded for the given traceback.
        /// </summary>
        Task<List<TracebackItem>> GetTracebackItemsAsync(string tracebackId);

        Task ClearDatabaseAsync();
    }
}
