using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriverService.Authentication;
using TraceabilityDriverService.Models;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models;
using TraceabilityEngine.Interfaces.Models.DigitalLink;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityDriverService.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class EventsController : ControllerBase
    {
        private readonly ITDConfiguration _configuration;

        public EventsController(ITDConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}/{epcStr}")]
        public async Task<ActionResult<string>> GetEvents(long accountID, long tradingPartnerID, string epcStr, [FromQuery] DateTime? minEventTime = null, [FromQuery] DateTime? maxEventTime = null)
        {
            try
            {
                if (string.IsNullOrEmpty(epcStr))
                {
                    return new BadRequestObjectResult("No EPC was provided.");
                }

                if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                {
                    return new BadRequestObjectResult($"The EPC {epc} is not valid. {error}");
                }

                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    ITEDriverAccount account = await driverDB.LoadAccountAsync(accountID);
                    if (account == null)
                    {
                        return new BadRequestObjectResult("The account is not known.");
                    }

                    ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(accountID, tradingPartnerID);
                    if (tp == null)
                    {
                        return new BadRequestObjectResult("The Trading Partner is not known.");
                    }

                    string authHeader = TradingPartnerRequestAuthorizer.GenerateAuthHeader(epc?.ToString(), account, tp);
                    string url = tp.DigitalLinkURL + $"/gtin/{epc.GTIN}/lot/{epc.SerialLotNumber}?linkType=gs1:epcis";

                    using (var item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                    {
                        HttpClient client = item.Value;
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Authorization", authHeader);
                        client.DefaultRequestHeaders.Add("x-pgln", account.PGLN?.ToString());
                        var response = await client.GetAsync(url);

                        // make sure that a link was returned to us successfully
                        if (response.IsSuccessStatusCode)
                        {
                            // validate we received the link type we are looking for
                            string linkJson = await response.Content.ReadAsStringAsync();
                            List<ITEDigitalLink> links = TraceabilityDriverServiceFactory.ParseLinks(linkJson);
                            if (links == null || !links.Exists(l => l.linkType == "gs1:epcis"))
                            {
                                return new BadRequestObjectResult("Failed to get EPCIS Query Interface URL link from GS1 Digital Link Resolver.");
                            }

                            // build the link
                            string epcisURL = links.Find(l => l.linkType == "gs1:epcis").link + "/queries/SimpleEventQuery";

                            // build the body of the request in a C# object
                            SimpleEventQuery seQuery = new SimpleEventQuery();
                            seQuery.query = new SimpleEventQuery_Query();
                            seQuery.query.MATCH_anyEPC.Add(epc?.ToString());
                            string bodyJSON = JsonConvert.SerializeObject(seQuery);
                            authHeader = TradingPartnerRequestAuthorizer.GenerateAuthHeader(bodyJSON, account, tp);
                            StringContent content = new StringContent(bodyJSON, Encoding.UTF8, "application/json");

                            // make a post against the EPCIS Query Controller "/queries/SimpleEventQuery" method
                            client.DefaultRequestHeaders.Clear();
                            client.DefaultRequestHeaders.Add("Authorization", authHeader);
                            client.DefaultRequestHeaders.Add("x-pgln", account.PGLN?.ToString());
                            var response2 = await client.PostAsync(epcisURL, content); // bad request 400
                            string gs1Format = await response2.Content.ReadAsStringAsync();

                            // convert the events from the EPCIS format to the local format
                            ITEEPCISMapper mapper = new EPCISJsonMapper_2_0();
                            ITEEPCISDocument data = mapper.ReadEPCISData(gs1Format);
                            string localFormat = _configuration.Mapper.ReadEPCISData(data);
                            return localFormat;
                        }
                        else
                        {
                            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
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
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
