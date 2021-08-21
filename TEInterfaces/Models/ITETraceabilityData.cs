using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;

namespace TraceabilityEngine.Interfaces.Models
{
    public interface ITEEPCISData
    {
        public List<ITEEvent> Events { get; set; }
        public List<ITEProduct> ProductDefinitions { get; set; }
        public List<ITELocation> Locations { get; set; }
        public List<ITETradingParty> TradingParties { get; set; }
        public void Merge(ITEEPCISData data);
    }
}
