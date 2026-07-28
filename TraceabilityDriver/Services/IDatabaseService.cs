using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Models.DB;
using TraceabilityDriver.Models.Mapping;
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

        /// <summary>
        /// Returns the most recent sync recorded under the given deployment version, or null when that
        /// version has never synced. Used to carry memory variables forward within one deployment version
        /// only, so a new version starts a full resync.
        /// </summary>
        Task<SyncHistoryItem?> GetLatestSyncAsync(string deploymentVersion);

        Task InitializeDatabase();

        /// <summary>
        /// Queries events synced under the currently configured deployment version merged with the
        /// tracebacked events (stored with a null deployment version), with the synced copy winning when
        /// the same event id appears in both.
        /// </summary>
        Task<EPCISQueryDocument> QueryEvents(EPCISQueryParameters options);

        /// <summary>
        /// Returns the element synced under the currently configured deployment version, falling back to
        /// the tracebacked master data (stored with a null deployment version), or null when the element
        /// is in neither.
        /// </summary>
        Task<IVocabularyElement?> QueryMasterData(string identifier);

        /// <summary>
        /// Upserts the synced events into the cache by (event key, deployment version) and reports which
        /// event ids were inserted versus updated. Events synced under other deployment versions are untouched.
        /// </summary>
        /// <remarks>
        /// The <see cref="IEvent.EventID"/> of each incoming event must carry the event key produced by
        /// <see cref="CommonEvent.GetEventKey"/>; the method replaces it with the real CBV 2.0 event hash
        /// (generated with the OpenTraceability EventHashGenerator) before the event is persisted, and
        /// stores the key alongside it. The merged common event of each key is persisted too, so later
        /// sync runs can merge additional source rows into the stored event via
        /// <see cref="GetCommonEventsAsync"/>.
        /// </remarks>
        /// <param name="events">The converted events to store; their EventID carries the event key.</param>
        /// <param name="deploymentVersion">The deployment version to stamp on the stored events.</param>
        /// <param name="commonEventsByKey">The merged common events keyed by event key string.</param>
        Task<DatabaseStoreResult> StoreEventsAsync(List<IEvent> events, string deploymentVersion, IReadOnlyDictionary<string, CommonEvent> commonEventsByKey);

        /// <summary>
        /// Returns the stored common events for the given event keys under the given deployment version,
        /// keyed by event key. Keys with no stored event, or whose stored common event cannot be
        /// deserialized, are absent from the result.
        /// </summary>
        Task<Dictionary<string, CommonEvent>> GetCommonEventsAsync(List<string> eventKeys, string deploymentVersion);

        /// <summary>
        /// Upserts the synced master data into the cache by (element id, deployment version) and reports
        /// which element ids were inserted versus updated. Elements synced under other deployment versions are untouched.
        /// </summary>
        Task<DatabaseStoreResult> StoreMasterDataAsync(List<IVocabularyElement> masterData, string deploymentVersion);

        /// <summary>
        /// Stores tracebacked events with a null deployment version and no event key. Traceback data is
        /// never updated: an event is skipped when its event id already exists under the current
        /// deployment version or as previously tracebacked data; skipped ids are reported in neither
        /// CreatedIds nor UpdatedIds. Events without an event id get one generated from their content.
        /// </summary>
        Task<DatabaseStoreResult> StoreTracebackEventsAsync(List<IEvent> events);

        /// <summary>
        /// Stores tracebacked master data with a null deployment version. Traceback data is never
        /// updated: an element is skipped when its element id already exists under the current deployment
        /// version or as previously tracebacked data; skipped ids are reported in neither CreatedIds nor
        /// UpdatedIds.
        /// </summary>
        Task<DatabaseStoreResult> StoreTracebackMasterDataAsync(List<IVocabularyElement> masterData);

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
