using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services.Queues
{
    /// <summary>
    /// The background job that executes a queued traceback.
    /// </summary>
    /// <remarks>
    /// This class is queue-framework agnostic: it is a plain DI class any queue backend can resolve
    /// and invoke. It deliberately does not catch exceptions — <see cref="IIngestionService"/> persists
    /// the failure on the traceback record and rethrows, so the exception reaches the queue backend and
    /// triggers its automatic retry. Each retry upserts the same record id (flipping it back to
    /// <see cref="TracebackStatus.InProgress"/>); if retries run out, the record remains
    /// <see cref="TracebackStatus.Failed"/> with the last error captured.
    /// </remarks>
    public class TracebackJob
    {
        private readonly IIngestionService _ingestionService;
        private readonly ILogger<TracebackJob> _logger;

        /// <summary>
        /// Creates a new traceback job.
        /// </summary>
        /// <param name="ingestionService">The service that executes and records tracebacks.</param>
        /// <param name="logger">The logger used for job diagnostics.</param>
        public TracebackJob(IIngestionService ingestionService, ILogger<TracebackJob> logger)
        {
            _ingestionService = ingestionService ?? throw new ArgumentNullException(nameof(ingestionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes the traceback against the pre-created record id.
        /// </summary>
        /// <param name="tracebackId">The id of the pre-created traceback record.</param>
        /// <param name="request">The traceback request to execute.</param>
        /// <param name="cancellationToken">Cancels the traceback; under Hangfire this token fires on server shutdown or job deletion.</param>
        public async Task ExecuteAsync(string tracebackId, TracebackRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Dequeued traceback {TracebackId} for execution.", tracebackId);

            await _ingestionService.IngestTracebackAsync(tracebackId, request, cancellationToken);
        }
    }
}
