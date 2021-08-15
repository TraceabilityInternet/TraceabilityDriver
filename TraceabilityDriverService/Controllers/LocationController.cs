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
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Util;
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

        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}/{gln}")]
        public async Task<ActionResult<string>> Get(long accountID, long tradingPartnerID, string gln)
        {
            try
            {
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

                            // now we have the link to the master data
                            // we assume that this API will require the same authorization headers as the GS1 Digital Link Resolver
                            string masterDataURL = links.Find(l => l.linkType == "gs1:masterData").link; // "url" to "link", added [0] index
                            client.DefaultRequestHeaders.Clear();
                            client.DefaultRequestHeaders.Add("Authorization", authHeader);
                            var response2 = await client.GetAsync(masterDataURL); // Edited url to masterDataURL

                            // ensure we got a successful response back
                            if (response2.IsSuccessStatusCode)
                            {
                                string gs1Format = await response2.Content.ReadAsStringAsync();
                                ITELocationMapper mapper = new LocationWebVocabMapper();
                                ITELocation location = mapper.ConvertToLocation(gs1Format);
                                string localFormat = _configuration.Mapper.MapToLocalLocations(new List<ITELocation>() { location });
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
