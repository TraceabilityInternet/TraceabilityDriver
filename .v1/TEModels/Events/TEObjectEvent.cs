using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Models.Events
{
    public class TEObjectEvent : TEEventBase, ITEObjectEvent
    {
        public TEEventType EventType => TEEventType.Object;
        public List<ITEEventProduct> Products { get; set; } = new List<ITEEventProduct>();
        public ITEEventILMD ILMD { get; set; }

        public void AddProduct(IEPC epc, TEMeasurement quantity)
        {
            this.Products.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = quantity,
                Type = EventProductType.Reference
            });
        }

        public void AddProduct(IEPC epc, double value, string uom)
        {
            this.Products.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = new TEMeasurement(value, uom),
                Type = EventProductType.Reference
            });
        }

        public void AddProduct(IEPC epc, double value)
        {
            this.Products.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = new TEMeasurement(value, "EA"),
                Type = EventProductType.Reference
            });
        }

        public void AddProduct(IEPC epc)
        {
            this.Products.Add(new TEEventProduct()
            {
                EPC = epc,
                Type = EventProductType.Reference
            });
        }
    }
}
