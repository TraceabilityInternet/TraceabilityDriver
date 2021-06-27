using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public interface ITEAssociationEvent : ITEEvent
    {
        IEPC ParentID { get; set; }
        List<ITEEventProduct> Children { get; set; }
        void AddChild(IEPC epc, TEMeasurement quantity);
        void AddChild(IEPC epc, double value, string uom);
        void AddChild(IEPC epc, double value);
        void AddChild(IEPC epc);
    }
}
