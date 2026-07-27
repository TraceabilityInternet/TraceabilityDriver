using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services.Queues
{
    /// <summary>
    /// Queues traceback requests for background execution.
    /// </summary>
    /// <remarks>
    /// This is the seam that keeps the rest of the application independent of the queue framework:
    /// callers pre-create a <see cref="TracebackRecord"/> (whose id is the public job id) and hand its
    /// id here, and the backend's own job identifiers never leak out. Swapping the queue framework
    /// (e.g. Hangfire for Azure Functions or an in-memory queue) means writing one new implementation
    /// of this interface and changing one registration. Implementations must ultimately execute the
    /// traceback by invoking <see cref="TracebackJob.ExecuteAsync"/> (or an equivalent path through
    /// <see cref="IIngestionService"/>) against the given record id.
    /// </remarks>
    public interface ITracebackQueue
    {
        /// <summary>
        /// Queues background execution of a traceback against a pre-created record.
        /// </summary>
        /// <param name="tracebackId">The id of the pre-created <see cref="TracebackStatus.Queued"/> traceback record.</param>
        /// <param name="request">The validated traceback request to execute.</param>
        Task EnqueueTracebackAsync(string tracebackId, TracebackRequest request);
    }
}
