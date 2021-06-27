using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;

namespace TraceabilityEngine.Models.Events
{
    public class TEEventReadPoint : ITEEventReadPoint
    {
        public string ID { get; set; }
    }
}
