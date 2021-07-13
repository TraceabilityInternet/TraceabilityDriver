using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Interfaces.Driver
{
    public interface ITEInternalClient : IDisposable
    {
        Task<ITEDriverAccount> GetAccountAsync(long id);

        Task<ITEDriverAccount> SaveAccountAsync(ITEDriverAccount account);

        Task<ITEDriverTradingPartner> GetTradingPartnerAsync(long accountID, long tradingPartnerID);

        Task<ITEDriverTradingPartner> AddTradingPartnerAsync(long accountID, IPGLN pgln);

        Task<ITEDriverTradingPartner> AddTradingPartnerManuallyAsync(long accountID, ITEDriverTradingPartner tp);

        Task DeleteTradingPartnerAsync(long accountID, long tradingPartnerID);

        Task<string> GetTradeItemAsync(long accountID, long tradingPartnerID, string gtin);

        Task<string> GetLocationAsync(long accountID, long tradingPartnerID, string gln);

        Task<string> GetTradingPartyAsync(long accountID, long tradingPartnerID, string pgln);

        Task<string> GetEventsAsync(long accountID, long tradingPartnerID, string epc);

        Task<string> GetEventsAsync(long accountID, long tradingPartnerID, string epc, DateTime minEventTime, DateTime maxEventTime);
    }
}
