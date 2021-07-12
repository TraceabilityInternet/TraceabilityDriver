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
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Service.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityDriverService.Controllers
{
    [ApiController]
    [Route("api/tradingparty")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class TradingPartyController
    {
        private readonly ITDConfiguration _configuration;

        public TradingPartyController(ITDConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}/{pgln}")]
        public async Task<string> Get(long accountID, long tradingPartnerID, string pgln)
        {
            using (ITEDriverDB driverDB = _configuration.GetDB())
            {
                ITEDriverAccount account = await driverDB.LoadAccountAsync(accountID);
                ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(accountID, tradingPartnerID);
                string authHeader = TradingPartnerRequestAuthorizer.GenerateAuthHeader(pgln, account, tp);
                string url = tp.DigitalLinkURL + $"/pgln/{pgln}?linkType=gs1:masterData";

                using (var item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                {
                    HttpClient client = item.Value;
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("Authorization", authHeader);
                    //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authHeader); // Looks like a bad header...
                    var response = await client.GetAsync(url);
                    string linkJson = await response.Content.ReadAsStringAsync();
                    JArray jArr = JArray.Parse(linkJson); // Edited Jobject to Jarray
                    string masterDataURL = jArr[0].Value<string>("link"); // "url" to "link", added [0] index

                    // now we have the link to the master data
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("Authorization", authHeader);
                    var response2 = await client.GetAsync(masterDataURL); // Edited url to masterDataURL
                    string gs1Format = await response2.Content.ReadAsStringAsync();
                    ITETradingPartyMapper mapper = new TradingPartyWebVocabMapper();
                    ITETradingParty theTP = mapper.ConvertToTradingParty(gs1Format);
                    string localFormat = _configuration.Mapper.MapToLocalTradingPartners(new List<ITETradingParty>() { theTP });
                    return localFormat;
                }
            }
        }
    }
}
