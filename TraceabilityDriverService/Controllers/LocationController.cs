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
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Service.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityDriverService.Controllers
{
    [ApiController]
    [Route("api/location")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class LocationController
    {
        private readonly ITDConfiguration _configuration;

        public LocationController(ITDConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// An HTTP GET request that returns, in local format, a Location, identified by a GLN, of a specified Account's Trading Partner.
        /// </summary>
        /// <param name="accountID"></param>
        /// <param name="tradingPartnerID"></param>
        /// <param name="gln"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}/{gln}")]
        public async Task<ActionResult<string>> Get(long accountID, long tradingPartnerID, string gln)
        {
            // validation
            if (string.IsNullOrEmpty(gln))
            {
                return new BadRequestObjectResult("GLN is null or empty");
            }

            if (tradingPartnerID == 0)
            {
                return new BadRequestObjectResult("Trading partner ID was not properly set");
            }

            if (accountID == 0)
            {
                return new BadRequestObjectResult("Account ID is null");
            }

            using (ITEDriverDB driverDB = _configuration.GetDB())
            {
                ITEDriverAccount account = await driverDB.LoadAccountAsync(accountID);
                ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(accountID, tradingPartnerID);
                string authHeader = TradingPartnerRequestAuthorizer.GenerateAuthHeader(gln, account, tp);
                string url = tp.DigitalLinkURL + $"/gln/{gln}?linkType=gs1:masterData";

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
                    ITELocationMapper mapper = new LocationWebVocabMapper();
                    ITELocation location = mapper.ConvertToLocation(gs1Format);
                    string localFormat = _configuration.Mapper.MapToLocalLocations(new List<ITELocation>() { location });
                    return localFormat;


                    // now we have the link to
                    //throw new NotImplementedException();
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
