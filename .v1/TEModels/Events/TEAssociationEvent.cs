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
    public class TEAssociationEvent : TEEventBase, ITEAssociationEvent
    {
        public IEPC ParentID { get; set; }
        public List<ITEEventProduct> Children { get; set; } = new List<ITEEventProduct>();
        public TEEventType EventType => TEEventType.Association;
        public ITEEventILMD ILMD { get; set; }
        public List<ITEEventProduct> Products
        {
            get
            {
                List<ITEEventProduct> products = new List<ITEEventProduct>();
                products.Add(new TEEventProduct()
                {
                    EPC = this.ParentID,
                    Type = EventProductType.Parent
                });
                products.AddRange(this.Children);
                return products;
            }
        }


        public void AddChild(IEPC epc, TEMeasurement quantity)
        {
            this.Children.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = quantity,
                Type = EventProductType.Child
            });
        }

        public void AddChild(IEPC epc, double value, string uom)
        {
            this.Children.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = new TEMeasurement(value, uom),
                Type = EventProductType.Child
            });
        }

        public void AddChild(IEPC epc, double value)
        {
            this.Children.Add(new TEEventProduct()
            {
                EPC = epc,
                Quantity = new TEMeasurement(value, "EA"),
                Type = EventProductType.Child
            });
        }

        public void AddChild(IEPC epc)
        {
            this.Children.Add(new TEEventProduct()
            {
                EPC = epc,
                Type = EventProductType.Child
            });
        }
    }
}
