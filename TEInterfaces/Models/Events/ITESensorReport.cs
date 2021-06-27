using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public interface ITESensorReport
    {
        DateTime? TimeStamp { get; set; }
        Uri Type { get; set; }
        TEMeasurement Measurement { get; set; }
    }
}
