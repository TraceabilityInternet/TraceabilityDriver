using Hangfire;
using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services.Queues
{
    /// <summary>
    /// Hangfire-backed implementation of <see cref="ITracebackQueue"/>.
    /// </summary>
    /// <remarks>
    /// Enqueues a <see cref="TracebackJob"/> through Hangfire's job client. The
    /// <see cref="CancellationToken"/> passed in the expression is a placeholder: Hangfire substitutes
    /// its own token at execution time, which fires on server shutdown or job deletion.
    /// </remarks>
    public class HangfireTracebackQueue : ITracebackQueue
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        /// <summary>
        /// Creates a new Hangfire traceback queue.
        /// </summary>
        /// <param name="backgroundJobClient">The Hangfire client used to enqueue jobs.</param>
        public HangfireTracebackQueue(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        }

        /// <inheritdoc/>
        public Task EnqueueTracebackAsync(string tracebackId, TracebackRequest request)
        {
            _backgroundJobClient.Enqueue<TracebackJob>(job => job.ExecuteAsync(tracebackId, request, CancellationToken.None));
            return Task.CompletedTask;
        }
    }
}
