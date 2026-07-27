using Extensions;
using MongoDB.Bson;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Orchestrates tracebacks and records what they ingest into the traceability data cache.
    /// </summary>
    public class IngestionService : IIngestionService
    {
        private const int StoreBatchSize = 100;

        private readonly ITracebackService _tracebackService;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<IngestionService> _logger;

        /// <summary>
        /// Creates a new ingestion service.
        /// </summary>
        /// <param name="tracebackService">The service that executes tracebacks against the external server.</param>
        /// <param name="databaseService">The traceability data cache.</param>
        /// <param name="logger">The logger used for ingestion diagnostics.</param>
        public IngestionService(ITracebackService tracebackService, IDatabaseService databaseService, ILogger<IngestionService> logger)
        {
            _tracebackService = tracebackService ?? throw new ArgumentNullException(nameof(tracebackService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<TracebackRecord> IngestTracebackAsync(TracebackRequest request, CancellationToken cancellationToken)
        {
            return await IngestTracebackAsync(ObjectId.GenerateNewId().ToString(), request, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TracebackRecord> CreateQueuedTracebackAsync(TracebackRequest request)
        {
            // Validate before opening a record so bad requests fail here, at request time, and never
            // leave a trace in the database or a job that fails silently in the queue.
            ValidatedTracebackRequest validated = ValidateRequest(request);

            TracebackRecord record = new TracebackRecord
            {
                Status = TracebackStatus.Queued,
                ResolverUrl = validated.ResolverUrl,
                RequestedEpcs = validated.Epcs
            };
            await _databaseService.StoreTracebackAsync(record);

            _logger.LogInformation("Traceback {TracebackId} queued against {ResolverUrl} for {EpcCount} EPC(s).", record.Id, validated.ResolverUrl, validated.Epcs.Count);

            return record;
        }

        /// <inheritdoc/>
        public async Task<TracebackRecord> IngestTracebackAsync(string tracebackId, TracebackRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(tracebackId))
            {
                throw new ArgumentException("A traceback record id is required.", nameof(tracebackId));
            }

            ValidatedTracebackRequest validated = ValidateRequest(request);

            // Open the record before fetching so an interrupted run still leaves an inspectable InProgress record.
            // Storing is an upsert by id, so a pre-created Queued record flips to InProgress here.
            TracebackRecord record = new TracebackRecord
            {
                Id = tracebackId,
                StartTime = DateTime.UtcNow,
                Status = TracebackStatus.InProgress,
                ResolverUrl = validated.ResolverUrl,
                RequestedEpcs = validated.Epcs
            };
            await _databaseService.StoreTracebackAsync(record);

            _logger.LogInformation("Traceback {TracebackId} started against {ResolverUrl} for {EpcCount} EPC(s).", record.Id, validated.ResolverUrl, validated.Epcs.Count);

            try
            {
                TracebackFetchResult fetchResult = await _tracebackService.TracebackAsync(validated.Epcs, validated.ResolverOptions, cancellationToken);
                record.Errors.AddRange(fetchResult.Errors);

                // Upsert everything into the cache, accumulating which ids were created versus updated.
                DatabaseStoreResult eventsResult = new DatabaseStoreResult();
                foreach (List<IEvent> batch in fetchResult.Document.Events.Batch(StoreBatchSize))
                {
                    eventsResult.Merge(await _databaseService.StoreEventsAsync(batch));
                }

                DatabaseStoreResult masterDataResult = new DatabaseStoreResult();
                foreach (List<IVocabularyElement> batch in fetchResult.Document.MasterData.Batch(StoreBatchSize))
                {
                    masterDataResult.Merge(await _databaseService.StoreMasterDataAsync(batch));
                }

                // Record the ledger: one entry per resource this run touched, keyed to the traceback record.
                List<TracebackItem> ledger = BuildLedgerItems(record.Id, eventsResult, masterDataResult);
                if (ledger.Count > 0)
                {
                    await _databaseService.StoreTracebackItemsAsync(ledger);
                }

                record.EventsCreated = eventsResult.CreatedIds.Count;
                record.EventsUpdated = eventsResult.UpdatedIds.Count;
                record.MasterDataCreated = masterDataResult.CreatedIds.Count;
                record.MasterDataUpdated = masterDataResult.UpdatedIds.Count;
                record.Status = record.Errors.Count > 0 ? TracebackStatus.CompletedWithErrors : TracebackStatus.Completed;

                _logger.LogInformation("Traceback {TracebackId} finished with status {Status}: {EventsCreated} event(s) created, {EventsUpdated} updated, {MasterDataCreated} master data element(s) created, {MasterDataUpdated} updated.", record.Id, record.Status, record.EventsCreated, record.EventsUpdated, record.MasterDataCreated, record.MasterDataUpdated);
            }
            catch (Exception ex)
            {
                // Record the failure, then rethrow so the queue backend can retry the job. The finally block
                // persists the Failed record, so the error is captured even if the retries eventually run out.
                _logger.LogError(ex, "Traceback {TracebackId} failed.", record.Id);
                record.Status = TracebackStatus.Failed;
                record.Errors.Add(ex.Message);
                throw;
            }
            finally
            {
                record.EndTime = DateTime.UtcNow;
                await _databaseService.StoreTracebackAsync(record);
            }

            return record;
        }

        /// <summary>
        /// Validates the request and materializes the resolver options a traceback run needs.
        /// </summary>
        /// <param name="request">The traceback request to validate. Cannot be null.</param>
        /// <returns>The cleaned EPC list, resolver URL, and resolver query options.</returns>
        /// <exception cref="ArgumentException">Thrown when the request has no EPCs or no valid resolver URL.</exception>
        private static ValidatedTracebackRequest ValidateRequest(TracebackRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            List<string> epcs = request.Epcs?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? new List<string>();
            if (epcs.Count == 0)
            {
                throw new ArgumentException("At least one EPC is required to run a traceback.", nameof(request));
            }

            string? resolverUrl = request.ResolverUrl;
            if (string.IsNullOrWhiteSpace(resolverUrl) || !Uri.TryCreate(resolverUrl, UriKind.Absolute, out Uri? resolverUri))
            {
                throw new ArgumentException("No valid resolver URL was provided in the request.", nameof(request));
            }

            DigitalLinkQueryOptions resolverOptions = new DigitalLinkQueryOptions
            {
                URL = resolverUri,
                APIKey = request.ApiKey,
                Format = EPCISDataFormat.JSON,
                Version = EPCISVersion.V2,
                ResolverVersion = ResolverVersion.ResolverStandard_1_2_0
            };

            return new ValidatedTracebackRequest(epcs, resolverUrl, resolverOptions);
        }

        /// <summary>
        /// Builds one ledger entry per resource the run created or updated.
        /// </summary>
        private static List<TracebackItem> BuildLedgerItems(string tracebackId, DatabaseStoreResult eventsResult, DatabaseStoreResult masterDataResult)
        {
            List<TracebackItem> items = new List<TracebackItem>();

            items.AddRange(eventsResult.CreatedIds.Select(id => new TracebackItem { TracebackId = tracebackId, ItemType = TracebackItemType.Event, ItemId = id, Created = true }));
            items.AddRange(eventsResult.UpdatedIds.Select(id => new TracebackItem { TracebackId = tracebackId, ItemType = TracebackItemType.Event, ItemId = id, Created = false }));
            items.AddRange(masterDataResult.CreatedIds.Select(id => new TracebackItem { TracebackId = tracebackId, ItemType = TracebackItemType.MasterData, ItemId = id, Created = true }));
            items.AddRange(masterDataResult.UpdatedIds.Select(id => new TracebackItem { TracebackId = tracebackId, ItemType = TracebackItemType.MasterData, ItemId = id, Created = false }));

            return items;
        }

        /// <summary>
        /// The validated pieces of a traceback request: the cleaned EPC list, the resolver URL, and the resolver query options.
        /// </summary>
        private sealed record ValidatedTracebackRequest(List<string> Epcs, string ResolverUrl, DigitalLinkQueryOptions ResolverOptions);
    }
}
