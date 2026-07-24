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
        /// <returns>The finalized traceback record, including counts of created/updated resources and any errors. A run that fails mid-way is returned with <see cref="TracebackStatus.Failed"/> rather than thrown.</returns>
        /// <exception cref="ArgumentException">Thrown when the request has no EPCs or no resolver URL can be determined.</exception>
        Task<TracebackRecord> IngestTracebackAsync(TracebackRequest request, CancellationToken cancellationToken);
    }
}
