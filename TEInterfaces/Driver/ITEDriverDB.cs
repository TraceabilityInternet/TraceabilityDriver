using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.DigitalLink;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Interfaces.Driver
{
    public interface ITEDriverDB : IDisposable
    {
        Task SaveAccountAsync(ITEDriverAccount account, string configURL);
        Task<ITEDriverAccount> LoadAccountAsync(long accountID);
        Task<ITEDriverAccount> LoadAccountAsync(IPGLN pgln);
        Task SaveTradingPartnerAsync(ITEDriverTradingPartner tradingPartner);
        Task<ITEDriverTradingPartner> LoadTradingPartnerAsync(long accountID, long tradingPartnerID);
        Task<ITEDriverTradingPartner> LoadTradingPartnerAsync(long accountID, IPGLN tpPGLN);
        Task DeleteTradingPartnerAsync(long accountID, long tradingPartnerID);
        Task<List<ITEDigitalLink>> LoadDigitalLinks();
        Task SaveDigitalLink(ITEDigitalLink link);
    }
}
