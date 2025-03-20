using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Models.Events
{
    public class TEEventProduct : ITEEventProduct
    {
        public IEPC EPC { get; set; }
        public TEMeasurement Quantity { get; set; }
        public EventProductType Type { get; set; }
    }
}
