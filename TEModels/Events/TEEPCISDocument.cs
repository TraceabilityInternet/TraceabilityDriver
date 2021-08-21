using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;

namespace TraceabilityEngine.Models.Events
{
    public class TEEPCISDocument : ITEEPCISDocument
    {
        public List<ITEEvent> Events { get; set; } = new List<ITEEvent>();
        public List<ITEProduct> ProductDefinitions { get; set; } = new List<ITEProduct>();
        public List<ITELocation> Locations { get; set; } = new List<ITELocation>();
        public List<ITETradingParty> TradingParties { get; set; } = new List<ITETradingParty>();

        public void Merge(ITEEPCISDocument data)
        {
            this.Events.AddRange(data.Events);
            this.ProductDefinitions.AddRange(data.ProductDefinitions);
            this.Locations.AddRange(data.Locations);
            this.TradingParties.AddRange(data.TradingParties);
        }
    }
}
