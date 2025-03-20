using System.Data;
using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services
{
    public interface IEventsTableMappingService
    {
        List<CommonEvent> MapEvents(TDEventMapping eventMapping, DataTable dataTable);
    }
}