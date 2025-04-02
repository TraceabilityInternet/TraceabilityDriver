
using TraceabilityDriver.Models.MongoDB;

namespace TraceabilityDriver.Services
{
    public interface ISynchronizeService
    {
        Task SynchronizeAsync(CancellationToken cancellationToken);
    }
}