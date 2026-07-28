
using TraceabilityDriver.Models.DB;

namespace TraceabilityDriver.Services
{
    public interface ISynchronizeService
    {
        Task SynchronizeAsync(CancellationToken cancellationToken);
    }
}