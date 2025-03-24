namespace TraceabilityDriver.Services
{
    public class HostedSyncService : IHostedService
    {
        private readonly ISynchronizeService _synchronizeService;
        private readonly ILogger<HostedSyncService> _logger;

        public HostedSyncService(ISynchronizeService synchronizeService, ILogger<HostedSyncService> logger)
        {
            _synchronizeService = synchronizeService;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting hosted sync service.");
            _synchronizeService.StartAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping hosted sync service.");
            _synchronizeService.StopAsync(cancellationToken);
            return Task.CompletedTask;
        }
    }
}