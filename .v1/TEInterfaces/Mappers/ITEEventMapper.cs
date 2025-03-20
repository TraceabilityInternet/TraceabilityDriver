using TraceabilityEngine.Interfaces.Models.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Mappers
{
    public interface ITEEventMapper
    {
        string ConvertFromEvents(List<ITEEvent> ctes, Dictionary<string, string> cbvMappings = null);
        List<ITEEvent> ConvertToEvents(string value);
    }
}
