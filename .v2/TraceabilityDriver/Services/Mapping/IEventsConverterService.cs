using OpenTraceability.Models.Events;
using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services
{
    public interface IEventsConverterService
    {
        Task<EPCISDocument> ConvertEventsAsync(List<CommonEvent> events);
    }
}