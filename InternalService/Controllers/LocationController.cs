using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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
    [Route("api/location")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class LocationController
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _directoryURL;
        private readonly IDID _serviceProvider;
        //private readonly ITETraceabilityDriver _driver;

        public LocationController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration["ConnectionString"];
            _directoryURL = _configuration["DirectoryURL"];
            _serviceProvider = DIDFactory.Parse(_configuration["ServiceProvider"]);
        }

        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}/{gln}")]
        public async Task<string> Get(long accountID, long tradingPartnerID, string gln)
        {
            using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
            {
                ITEDriverAccount account = await driverDB.LoadAccountAsync(accountID);
                ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(accountID, tradingPartnerID);
                string authHeader = TradingPartnerRequestAuthorizer.GenerateAuthHeader(gln, account, tp);
                string url = tp.DigitalLinkURL + $"/gln/{gln}?linkType=gs1:masterData";

                using (var item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                {
                    HttpClient client = item.Value;
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authHeader);
                    var response = await client.GetAsync(url);
                    string linkJson = await response.Content.ReadAsStringAsync();
                    JObject jObj = JObject.Parse(linkJson);
                    string masterDataURL = jObj.Value<string>("url");

                    // now we have the link to
                    throw new NotImplementedException();
                    //client.DefaultRequestHeaders.Clear();
                    //var response2 = await client.GetAsync(url);
                    //string gs1Format = await response2.Content.ReadAsStringAsync();
                    //string localFormat = _driver.MapToLocalLocations(gs1Format);
                    //return localFormat;
                }
            }
        }
    }
}
