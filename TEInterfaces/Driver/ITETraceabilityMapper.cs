using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;

namespace TraceabilityEngine.Interfaces.Driver
{
    public interface ITETraceabilityMapper
    {
        string MapToLocalEvents(List<ITEEvent> gs1Events, Dictionary<string, object> parameters);
        List<ITEEvent> MapToGS1Events(string localEvents, Dictionary<string, object> parameters);

        string MapToLocalTradeItems(List<ITEProduct> products);
        List<ITEProduct> MapToGS1TradeItems(string localTradeItems);

        string MapToLocalLocations(List<ITELocation> gs1Locations);
        List<ITELocation> MapToGS1Locations(string localLocations);

        string MapToLocalTradingPartners(List<ITETradingParty> tradingParties);
        List<ITETradingParty> MapToGS1TradingPartners(string localTradingPartners);
    }
}
