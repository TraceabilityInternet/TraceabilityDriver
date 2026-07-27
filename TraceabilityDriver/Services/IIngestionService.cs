using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Orchestrates tracebacks and records their results in the traceability data cache.
    /// </summary>
    /// <remarks>
    /// The ingestion flow is: open a <see cref="TracebackRecord"/>, execute the traceback through
    /// <see cref="ITracebackService"/>, upsert the returned events and master data into the cache,
    /// write a <see cref="TracebackItem"/> ledger entry for every resource the run created or updated,
    /// and finalize the record with counts, errors, and a status. Ingestion is idempotent: the cache
    /// upserts by natural id, so repeating a traceback over the same products updates in place instead
    /// of duplicating, and the ledger's unique key makes retried writes duplicate-free. The driver only
    /// records what was ingested — it never writes to any customer-internal database.
    /// </remarks>
    public interface IIngestionService
    {
        /// <summary>
        /// Executes the requested traceback and ingests the results into the data cache.
        /// </summary>
        /// <param name="request">The traceback request. Must contain at least one EPC and a resolver URL (from the request or configuration).</param>
        /// <param name="cancellationToken">Cancels the traceback between EPCs.</param>
        /// <returns>The finalized traceback record, including counts of created/updated resources and any errors. Per-EPC fetch errors are recorded on the record as <see cref="TracebackStatus.CompletedWithErrors"/> rather than thrown.</returns>
        /// <exception cref="ArgumentException">Thrown when the request has no EPCs or no resolver URL can be determined.</exception>
        /// <exception cref="Exception">Unhandled failures mid-run are persisted on the record as <see cref="TracebackStatus.Failed"/> and then rethrown so a queue backend can retry the job.</exception>
        Task<TracebackRecord> IngestTracebackAsync(TracebackRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the requested traceback against a pre-created record id and ingests the results into the data cache.
        /// </summary>
        /// <remarks>
        /// This is the queued execution path: the record was pre-created by <see cref="CreateQueuedTracebackAsync"/>
        /// and its id handed to the queue, so the run upserts the same record (flipping it from
        /// <see cref="TracebackStatus.Queued"/> to <see cref="TracebackStatus.InProgress"/>). Retried jobs are safe:
        /// every attempt upserts the same record id.
        /// </remarks>
        /// <param name="tracebackId">The id of the pre-created traceback record to execute against.</param>
        /// <param name="request">The traceback request. Must contain at least one EPC and a resolver URL.</param>
        /// <param name="cancellationToken">Cancels the traceback between EPCs.</param>
        /// <returns>The finalized traceback record, including counts of created/updated resources and any errors.</returns>
        /// <exception cref="ArgumentException">Thrown when the id is blank or the request has no EPCs or no valid resolver URL.</exception>
        /// <exception cref="Exception">Unhandled failures mid-run are persisted on the record as <see cref="TracebackStatus.Failed"/> and then rethrown so a queue backend can retry the job.</exception>
        Task<TracebackRecord> IngestTracebackAsync(string tracebackId, TracebackRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Validates the request and pre-creates a <see cref="TracebackStatus.Queued"/> traceback record without executing anything.
        /// </summary>
        /// <remarks>
        /// Validation happens here, at request time, so a bad request fails immediately instead of failing
        /// silently inside a queued job. The returned record's id is the public job id.
        /// </remarks>
        /// <param name="request">The traceback request. Must contain at least one EPC and a resolver URL.</param>
        /// <returns>The stored record with <see cref="TracebackStatus.Queued"/> status and no start time.</returns>
        /// <exception cref="ArgumentException">Thrown when the request has no EPCs or no valid resolver URL. Nothing is stored in that case.</exception>
        Task<TracebackRecord> CreateQueuedTracebackAsync(TracebackRequest request);
    }
}
