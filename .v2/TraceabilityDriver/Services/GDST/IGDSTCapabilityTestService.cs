using OpenTraceability.Models.Events;
using TraceabilityDriver.Models.GDST;

namespace TraceabilityDriver.Services.GDST
{
    public interface IGDSTCapabilityTestService
    {
        Task<GDSTCapabilityTestResults> TestFirstMileWildAsync();
    }
}