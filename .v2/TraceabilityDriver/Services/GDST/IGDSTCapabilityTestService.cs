using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Services.GDST
{
    public interface IGDSTCapabilityTestService
    {
        Task<bool> ExecuteTestAsync();
        EPCISDocument GenerateTraceabilityData();
        Task LoadTestDataIntoDatabaseAsync();
        Task<bool> TestFirstMileWildAsync();
    }
}