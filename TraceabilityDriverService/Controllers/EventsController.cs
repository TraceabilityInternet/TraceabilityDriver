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
using TraceabilityDriverService.Models;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Service.Util;
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
            if (string.IsNullOrEmpty(epcStr))
            {
                return new BadRequestObjectResult("No EPC was provided.");
            }

            if(!EPC.TryParse(epcStr, out IEPC epc, out string error))
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
                    var response = await client.GetAsync(url);

                    // make sure that a link was returned to us successfully
                    if (response.IsSuccessStatusCode)
                    {
                        string linkJson = await response.Content.ReadAsStringAsync();
                        //JObject jObj = JObject.Parse(linkJson);
                        //string epcisURL = jObj.Value<string>("url") + "/queries/SimpleEventQuery";
                        JArray jArr = JArray.Parse(linkJson);
                        string epcisURL = jArr[0].Value<string>("link") + "/queries/SimpleEventQuery";

                        // now we have the link to the master data
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Authorization", authHeader);

                        // build the body of the request in a C# object
                        SimpleEventQuery seQuery = new SimpleEventQuery();
                        seQuery.query = new SimpleEventQuery_Query();
                        seQuery.query.MATCH_anyEPC.Add(epc?.ToString());
                        string bodyJSON = JsonConvert.SerializeObject(seQuery);
                        authHeader = TradingPartnerRequestAuthorizer.GenerateAuthHeader(bodyJSON, account, tp);
                        StringContent content = new StringContent(bodyJSON, Encoding.UTF8, "application/json");

                        // make a post against the EPCIS Query Controller "/queries/SimpleEventQuery" method
                        var response2 = await client.PostAsync(epcisURL, content); // bad request 400
                        string gs1Format = await response2.Content.ReadAsStringAsync();

                        // convert the events from the EPCIS format to the local format
                        ITEEventMapper mapper = new EPCISJsonMapper_2_0();
                        List<ITEEvent> events = mapper.ConvertToEvents(gs1Format);
                        string localFormat = _configuration.Mapper.MapToLocalEvents(events, null);
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
    }
}
