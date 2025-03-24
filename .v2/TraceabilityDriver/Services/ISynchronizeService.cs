
namespace TraceabilityDriver.Services
{
    public interface ISynchronizeService : IHostedService
    {
        Task SynchronizeAsync(CancellationToken cancellationToken);
    }
}