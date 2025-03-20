using TraceabilityEngine.Interfaces.Models.TradingParty;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Mappers
{
    public interface ITETradingPartyMapper
    {
        string ConvertFromTradingParty(ITETradingParty tp);
        ITETradingParty ConvertToTradingParty(string json);
    }
}
