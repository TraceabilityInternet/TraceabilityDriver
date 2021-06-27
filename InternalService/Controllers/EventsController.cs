using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Service.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace InternalService.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class EventsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _directoryURL;
        private readonly IDID _serviceProvider;
        //private readonly ITETraceabilityDriver _driver;

        public EventsController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration["ConnectionString"];
            _directoryURL = _configuration["DirectoryURL"];
            _serviceProvider = DIDFactory.Parse(_configuration["ServiceProvider"]);
        }

        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}/{epc}")]
        public async Task<string> GetEvents(long accountID, long tradingPartnerID, string epc)
        {
            using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
            {
                ITEDriverAccount account = await driverDB.LoadAccountAsync(accountID);
                ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(accountID, tradingPartnerID);
                string authHeader = TradingPartnerRequestAuthorizer.GenerateAuthHeader(epc, account, tp);
                string url = tp.EPCISQueryURL + "/epcs/" + epc;

                using (var item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                {
                    HttpClient client = item.Value;
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authHeader);
                    var response = await client.GetAsync(url);
                    throw new NotImplementedException();
                    //string gs1Events = await response.Content.ReadAsStringAsync();
                    //string localFormat = _driver.MapToLocalEvents(gs1Events);
                    //return localFormat;
                }
            }
        }
    }
}
