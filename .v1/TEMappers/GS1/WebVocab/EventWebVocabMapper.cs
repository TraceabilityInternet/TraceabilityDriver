using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Mappers;

namespace TEMappers.GS1.WebVocab
{
    public class EventWebVocabMapper : GS1WebVocabMapper, ITEEventMapper
    {
        public string ConvertFromEvents(List<ITEEvent> ctes, Dictionary<string, string> cbvMappings = null)
        {
            string events = null;
            return events;
        }
        public List<ITEEvent> ConvertToEvents(string value)
        {
            List<ITEEvent> events = new List<ITEEvent>();
            return events;
        }
    }
}
