
namespace TraceabilityDriver.Services
{
    public interface ISynchronizeService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}