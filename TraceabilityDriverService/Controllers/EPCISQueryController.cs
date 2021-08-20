using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TraceabilityDriverService.Authentication;
using TraceabilityDriverService.Models;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.ObjectPooling;
using TraceabilityEngine.Util.Security;

namespace TraceabilityDriverService.Controllers
{
    /// <summary>
    /// EPCIS Query Controller partially implementing the REST API interface from the EPCIS 2.0 Draft.
    /// </summary>
    [ApiController]
    [Route("{accountID}/epcis")]
    public class EPCISQueryController : ControllerBase
    {
        private ITDConfiguration _configuration;

        public EPCISQueryController(ITDConfiguration config)
        {
            _configuration = config;
        }

        [HttpPost]
        [Route("queries/SimpleEventQuery")]
        public async Task<IActionResult> GetEvents([FromBody] SimpleEventQuery query, long accountID)
        {
            try
            {
                List<ITEEvent> events = new List<ITEEvent>();

                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    // here we are going to ensure that they are allowed to make this request
                    string queryString = JsonConvert.SerializeObject(query);
                    if (!_configuration.RequiresTradingPartnerAuthorization || await TradingPartnerRequestAuthorizer.Authorize(Request, queryString, driverDB, accountID))
                    {
                        // we need the tradingpartner_id because we pass that forward to the solution provider
                        // so that they can provide their own tradingpartner level permissioning and know who is
                        // making the request
                        long tradingpartner_id = await TradingPartnerRequestAuthorizer.GetTradingPartnerID(Request, driverDB, accountID);

                        // we will make a seperate request to the solution provider foreach epc that is being requested
                        foreach (string epc in query.query.MATCH_anyEPC)
                        {
                            // query the configured url for the epc
                            string url = _configuration.EventURLTemplate.Replace("{epc}", epc).Replace("{account_id}", accountID.ToString()).Replace("{tradingpartner_id}", tradingpartner_id.ToString());

                            // we need to use this pool for http clients since they can block ports if you don't pool them
                            using (LimitedPoolItem<HttpClient> item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                            {
                                HttpClient client = item.Value;

                                // if the solution provider has provided an API Key to use when communicating with it's API, then
                                // we need to provide that in the Authorization Header
                                client.DefaultRequestHeaders.Clear();
                                if (!string.IsNullOrWhiteSpace(_configuration.SolutionProviderAPIKey))
                                {
                                    client.DefaultRequestHeaders.Add("Authorization", "Basic " + _configuration.APIKey);
                                }

                                // get the results from the solution provider and map them from the solution provider's local format
                                // into the common data model and add those events to the list of events that we will return.
                                var response = await client.GetAsync(url);
                                string localData = await response.Content.ReadAsStringAsync();
                                List<ITEEvent> theseEvents = _configuration.Mapper.MapToGS1Events(localData);
                                events.AddRange(theseEvents);
                            }
                        }
                    }
                    else
                    {
                        return new UnauthorizedResult();
                    }
                }

                // take the events that we have collected and map them from the common data model into the EPCIS 2.0 JSON format
                // and return that data format.
                ITEEventMapper mapper = new EPCISJsonMapper_2_0();
                string gs1Events = mapper.ConvertFromEvents(events);
                return new OkObjectResult(gs1Events);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}

