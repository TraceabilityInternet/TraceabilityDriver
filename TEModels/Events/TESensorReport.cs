using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Models.Events
{
    public class TESensorReport : ITESensorReport
    {
        public DateTime? TimeStamp { get; set; }
        public Uri Type { get; set; }
        public TEMeasurement Measurement { get; set; }
    }
}
