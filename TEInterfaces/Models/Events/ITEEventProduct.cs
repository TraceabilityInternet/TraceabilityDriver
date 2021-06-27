using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public enum EventProductType
    {
        Reference = 1,
        Input = 2,
        Output = 3,
        Parent = 4,
        Child = 5
    };

    public interface ITEEventProduct
    {
        EventProductType Type { get; set; }
        IEPC EPC { get; set; }
        TEMeasurement Quantity { get; set; }
    }
}
