using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TraceabilityDriverService.Models
{
    public class SimpleEventQuery
    {
        public SimpleEventQuery_Query query { get; set; }
        public string queryType { get; set; } = "events";
    }

    public class SimpleEventQuery_Query
    {
        public List<string> MATCH_anyEPC { get; set; } = new List<string>();
    }
}
