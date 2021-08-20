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
using TraceabilityDriverService.Authentication;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.DigitalLink;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityDriverService.Controllers
{
    /// <summary>
    /// This is an API Controller that is used internall by the solution provider to request Trade Items
    /// from trading partners on behalf of their accounts.
    /// </summary>
    [ApiController]
    [Route("api/tradeitems")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class TradeItemController : ControllerBase
    {
        private readonly ITDConfiguration _configuration;

        public TradeItemController(ITDConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}/{gtin}")]
        public async Task<ActionResult<string>> Get(long accountID, long tradingPartnerID, string gtin)
        {
            try
            {
                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    // the first step is to query the GS1 Digital Link Resolver of the trading partner to get a link
                    // to the master data that is being requested.
                    ITEDriverAccount account = await driverDB.LoadAccountAsync(accountID);
                    ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(accountID, tradingPartnerID);
                    string authHeader = TradingPartnerRequestAuthorizer.GenerateAuthHeader(gtin, account, tp);
                    string url = tp.DigitalLinkURL + $"/gtin/{gtin}?linkType=gs1:masterData";

                    // we need to use the Http Client Pool because these clients block ports for awhile so it's better to pool them
                    using (var item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                    {
                        HttpClient client = item.Value;
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Authorization", authHeader);
                        client.DefaultRequestHeaders.Add("x-pgln", account.PGLN?.ToString());
                        var response = await client.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            // validate we received the link type we are looking for
                            string linkJson = await response.Content.ReadAsStringAsync();
                            List<ITEDigitalLink> links = TraceabilityDriverServiceFactory.ParseLinks(linkJson);
                            if (links == null || !links.Exists(l => l.linkType == "gs1:masterData"))
                            {
                                return new BadRequestObjectResult("Failed to get gs1:masterData link from GS1 Digital Link Resolver.");
                            }

                            // now we have the link to the master data api that will provide us the trade items
                            // we assume that this API will require the same authorization headers as the GS1 Digital Link Resolver
                            string masterDataURL = links.Find(l => l.linkType == "gs1:masterData").link; 
                            client.DefaultRequestHeaders.Clear();
                            client.DefaultRequestHeaders.Add("Authorization", authHeader);
                            client.DefaultRequestHeaders.Add("x-pgln", account.PGLN?.ToString());
                            var response2 = await client.GetAsync(masterDataURL); // Edited url to masterDataURL

                            // ensure we got a successful response back
                            if (response2.IsSuccessStatusCode)
                            {
                                // now we take the results, and map it from the GS1 Web Vocab JSON format into the common data model
                                string gs1Format = await response2.Content.ReadAsStringAsync();
                                ITEProductMapper mapper = new ProductWebVocabMapper();
                                ITEProduct product = mapper.ConvertToProduct(gs1Format);

                                // then we map the common data model into the solution providers local format and return that 
                                string localFormat = _configuration.Mapper.MapToLocalTradeItems(new List<ITEProduct>() { product });
                                return localFormat;
                            }
                            else
                            {
                                return new BadRequestObjectResult("There was an error with the link provided by the GS1 Digital Link Resolver.");
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            return new NotFoundResult();
                        }
                        else
                        {
                            return new BadRequestObjectResult("There was an unknown error processing this request.");
                        }
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
