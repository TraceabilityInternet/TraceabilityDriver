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
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Service.Util;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;
namespace TraceabilityDriverService.Controllers
{
    [ApiController]
    [Route("api/tradingparty")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class TradingPartyController : ControllerBase
    {
        private readonly ITDConfiguration _configuration;
        public TradingPartyController(ITDConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// An HTTP GET request that returns, in local format, a Trading Party, identified by a PGLN, of a specified Account's Trading Partner.
        /// </summary>
        /// <param name="accountID"></param>
        /// <param name="tradingPartnerID"></param>
        /// <param name="pgln"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}/{pgln}")]
        public async Task<ActionResult<string>> Get(long accountID, long tradingPartnerID, string pgln)
        {
            try
            {
                // validation
                if (accountID == 0)
                {
                    return "Account ID was not properly set.";
                }

                if(tradingPartnerID == 0)
                {
                    return "Trading Partner ID was not properly set.";
                }

                if (string.IsNullOrEmpty("pgln"))
                {
                    return "PGLN is null or empty.";
                }


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
                        ITETradingParty tradingParty = mapper.ConvertToTradingParty(gs1Format);
                        string localFormat = _configuration.Mapper.MapToLocalTradingPartners(new List<ITETradingParty>() { tradingParty });
                        return localFormat;
                    }
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}