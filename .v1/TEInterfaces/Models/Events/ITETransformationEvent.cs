using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public interface ITETransformationEvent : ITEEvent
    {
        string TransformationID { get; set; }
        List<ITEEventProduct> Inputs { get; set; }
        List<ITEEventProduct> Outputs { get; set; }

        void AddInput(IEPC epc, TEMeasurement quantity);
        void AddInput(IEPC epc, double value, string uom);
        void AddInput(IEPC epc, double value);
        void AddInput(IEPC epc);

        void AddOutput(IEPC epc, TEMeasurement quantity);
        void AddOutput(IEPC epc, double value, string uom);
        void AddOutput(IEPC epc, double value);
        void AddOutput(IEPC epc);
    }
}
