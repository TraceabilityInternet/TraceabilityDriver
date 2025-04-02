namespace TraceabilityDriver.Services
{
    public class HostedSyncService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<HostedSyncService> _logger;

        public HostedSyncService(IServiceProvider services, ILogger<HostedSyncService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Execute the synchronization process at midnight every night or if it has not processed in the last 24 hours
            // then execute it immediately.
            _logger.LogInformation("Starting hosted synchronize service...");

            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    using IServiceScope scope = _services.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<ISynchronizeService>();

                    try
                    {
                        _logger.LogInformation("Starting synchronization process...");

                        await syncService.SynchronizeAsync(cancellationToken);

                        _logger.LogInformation("Synchronization process completed.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred during synchronization process.");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping hosted sync service.");
            return Task.CompletedTask;
        }
    }
}