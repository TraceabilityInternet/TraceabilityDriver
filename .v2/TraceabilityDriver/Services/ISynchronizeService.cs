
using TraceabilityDriver.Models.MongoDB;

namespace TraceabilityDriver.Services
{
    public interface ISynchronizeService
    {
        SyncHistoryItem? CurrentSync { get; }

        event OnSynchronizeStatusChanged? OnSynchronizeStatusChanged;

        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
        Task SynchronizeAsync(CancellationToken cancellationToken);
    }
}