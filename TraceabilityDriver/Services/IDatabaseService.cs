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
        /// stores the key alongside it. The incoming event object is left carrying that generated id.
        ///
        /// The rows of one event can straddle sync runs, so an event already stored under the same key is
        /// enriched with the incoming one rather than replaced by it: the stored copy is the merge target and
        /// its values win conflicts, because they came from earlier rows. Correcting data already synced under
        /// a deployment version therefore means bumping the version, which forces a full resync. The stored
        /// event id changes whenever the merge changes the event's content.
        /// </remarks>
        /// <param name="events">The converted events to store; their EventID carries the event key.</param>
        /// <param name="deploymentVersion">The deployment version to stamp on the stored events.</param>
        Task<DatabaseStoreResult> StoreEventsAsync(List<IEvent> events, string deploymentVersion);

        /// <summary>
        /// Upserts the synced master data into the cache by (element id, deployment version) and reports
        /// which element ids were inserted versus updated. Elements synced under other deployment versions are untouched.
        /// </summary>
        /// <remarks>
        /// As with <see cref="StoreEventsAsync"/>, the rows describing one element can straddle sync runs, so
        /// an element already stored under the same id is enriched with the incoming one rather than replaced
        /// by it, with the stored copy's values winning conflicts.
        /// </remarks>
        Task<DatabaseStoreResult> StoreMasterDataAsync(List<IVocabularyElement> masterData, string deploymentVersion);

        /// <summary>
        /// Stores tracebacked events with a null deployment version and no event key. Traceback data is
        /// never updated: an event is skipped when a record with its event id already exists in the cache,
        /// regardless of deployment version; skipped ids are reported in neither CreatedIds nor
        /// UpdatedIds. Events without an event id get one generated from their content.
        /// </summary>
        /// <remarks>
        /// The existence check deliberately ignores the deployment version. Data the driver synced under an
        /// old version may have been pulled into another solution; when that solution is tracebacked, the
        /// superseded copy comes back and must not be written into the cache as traceback data.
        /// </remarks>
        Task<DatabaseStoreResult> StoreTracebackEventsAsync(List<IEvent> events);

        /// <summary>
        /// Stores tracebacked master data with a null deployment version. Traceback data is never
        /// updated: an element is skipped when a record with its element id already exists in the cache,
        /// regardless of deployment version; skipped ids are reported in neither CreatedIds nor
        /// UpdatedIds.
        /// </summary>
        /// <remarks>
        /// As with <see cref="StoreTracebackEventsAsync"/>, the existence check ignores the deployment
        /// version so a superseded copy that was tracebacked back to us is never re-saved.
        /// </remarks>
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
