using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Models.Events
{
    public class TETransformationEvent : TEEventBase, ITETransformationEvent
    {
        public TEEventType EventType => TEEventType.Transformation;
        public string TransformationID { get; set; }
        public List<ITEEventProduct> Inputs { get; set; } = new List<ITEEventProduct>();
        public List<ITEEventProduct> Outputs { get; set; } = new List<ITEEventProduct>();
        public ITEEventILMD ILMD { get; set; }

        public List<ITEEventProduct> Products 
        {
            get
            {
                List<ITEEventProduct> products = new List<ITEEventProduct>();
                products.AddRange(Inputs);
                products.AddRange(Outputs);
                return products;
            }
        }

        public void AddInput(IEPC epc, TEMeasurement quantity)
        {
            this.Inputs.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = quantity,
                Type = EventProductType.Input
            });
        }

        public void AddInput(IEPC epc, double value, string uom)
        {
            this.Inputs.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = new TEMeasurement(value, uom),
                Type = EventProductType.Input
            });
        }

        public void AddInput(IEPC epc, double value)
        {
            this.Inputs.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = new TEMeasurement(value, "EA"),
                Type = EventProductType.Input
            });
        }

        public void AddInput(IEPC epc)
        {
            this.Inputs.Add(new TEEventProduct()
            {
                EPC = epc,
                Type = EventProductType.Input
            });
        }

        public void AddOutput(IEPC epc, TEMeasurement quantity)
        {
            this.Outputs.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = quantity,
                Type = EventProductType.Output
            });
        }

        public void AddOutput(IEPC epc, double value, string uom)
        {
            this.Outputs.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = new TEMeasurement(value, uom),
                Type = EventProductType.Output
            });
        }

        public void AddOutput(IEPC epc, double value)
        {
            this.Outputs.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = new TEMeasurement(value, "EA"),
                Type = EventProductType.Output
            });
        }

        public void AddOutput(IEPC epc)
        {
            this.Outputs.Add(new TEEventProduct()
            {
                EPC = epc,
                Type = EventProductType.Output
            });
        }
    }
}
