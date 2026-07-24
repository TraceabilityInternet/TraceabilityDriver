using OpenTraceability.Queries;
using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Executes tracebacks against an external traceability server.
    /// </summary>
    /// <remarks>
    /// This service is a pure fetch: it talks HTTP to the external server through the OpenTraceability
    /// helpers and returns the merged results, but never touches the database. Persistence and ledger
    /// bookkeeping belong to <see cref="IIngestionService"/>, which keeps this service trivially testable
    /// and reusable. Failures on individual EPCs are collected as errors rather than thrown, so a partial
    /// traceback still yields usable data.
    /// </remarks>
    public interface ITracebackService
    {
        /// <summary>
        /// Traces each EPC back through the external server and returns the merged events and master data.
        /// </summary>
        /// <param name="epcs">The EPCs to trace back. Cannot be null.</param>
        /// <param name="resolverOptions">The digital link resolver options identifying the external server. Cannot be null.</param>
        /// <param name="cancellationToken">Cancels the traceback between EPCs.</param>
        /// <returns>The merged document (deduplicated by event id) and any per-EPC or master data resolution errors.</returns>
        Task<TracebackFetchResult> TracebackAsync(List<string> epcs, DigitalLinkQueryOptions resolverOptions, CancellationToken cancellationToken);
    }
}
