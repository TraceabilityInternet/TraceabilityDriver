using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public interface ITEObjectEvent : ITEEvent
    {
        new List<ITEEventProduct> Products { get; set; }
        void AddProduct(IEPC epc, TEMeasurement quantity);
        void AddProduct(IEPC epc, double value, string uom);
        void AddProduct(IEPC epc, double value);
        void AddProduct(IEPC epc);
    }
}
