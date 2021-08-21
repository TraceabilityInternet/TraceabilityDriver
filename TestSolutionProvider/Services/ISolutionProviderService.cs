using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;

namespace TestSolutionProvider.Services
{
    public interface ISolutionProviderService
    {
        public long AccountID { get; set; }
        public long TradingPartnerID { get; set; }
        public string DataURL { get; set; }
        public ITETraceabilityMapper Mapper { get; set; }

        public Task<ITEEPCISDocument> GetEventsAsync();
        public Task<List<ITEProduct>> GetProductsAsync();
        public Task<List<ITELocation>> GetLocationsAsync();
        public Task<List<ITETradingParty>> GetTradingPartiesAsync();

        public Task<string> GetRawData(string identifier);
        public Task RequestData(string epc);
    }
}
