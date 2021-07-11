using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityDriverService.Services.Interfaces
{
    public interface ITDConfiguration
    {
        ITETraceabilityMapper Mapper { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
        string URL { get; set; }
        string DirectoryURL { get; set; }
        IDID ServiceProviderDID { get; set; }
        IPGLN ServiceProviderPGLN { get; set; }
        string APIKey { get; set; }
        string MapperDLLPath { get; set; }
        string MapperClassName { get; set; }
        bool RequiresTradingPartnerAuthorization { get; set; }
        string EventURLTemplate { get; set; }
        string TradeItemURLTemplate { get; set; }
        string LocationURLTemplate { get; set; }
        string TradingPartnerURLTemplate { get; set; }
        ITEDriverDB GetDB();
    }
}
