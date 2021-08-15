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
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Util;
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
        public async Task<ActionResult<string>> Get(long accountID, long tradingPartnerID, string pgln)
        {
            try
            {
                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    // the first step is to query the GS1 Digital Link Resolver of the trading partner to get a link
                    // to the master data that is being requested.
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
                            var response2 = await client.GetAsync(masterDataURL); // Edited url to masterDataURL

                            // if we got a successful response, map the response into the local format and return that.
                            if (response2.IsSuccessStatusCode)
                            {
                                string gs1Format = await response2.Content.ReadAsStringAsync();
                                ITETradingPartyMapper mapper = new TradingPartyWebVocabMapper();
                                ITETradingParty theTP = mapper.ConvertToTradingParty(gs1Format);
                                string localFormat = _configuration.Mapper.MapToLocalTradingPartners(new List<ITETradingParty>() { theTP });
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
