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
using TraceabilityDriverService.Models;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Service.Util;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.ObjectPooling;
using TraceabilityEngine.Util.Security;

namespace TraceabilityDriverService.Controllers
{
    [ApiController]
    [Route("{accountID}/epcis")]
    public class EPCISQueryController : ControllerBase
    {
        private ITDConfiguration _configuration;

        public EPCISQueryController(ITDConfiguration config)
        {
            _configuration = config;
        }

        /// <summary>
        /// An HTTP POST request that returns, in GS1 format, a queried list of events.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="accountID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("queries/SimpleEventQuery")]
        public async Task<IActionResult> GetEvents([FromBody] SimpleEventQuery query, long accountID)
        {
            try
            {
                // validation
                if (query == null)
                {
                    return new BadRequestObjectResult("Query is null.");
                }

                if (accountID == 0)
                {
                    return new BadRequestObjectResult("Account ID was not properly set");
                }

                List<ITEEvent> events = new List<ITEEvent>();

                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    string queryString = JsonConvert.SerializeObject(query);
                    string authHeader = Request?.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (!_configuration.RequiresTradingPartnerAuthorization || await TradingPartnerRequestAuthorizer.Authorize(authHeader, queryString, driverDB, accountID))
                    {
                        long tradingpartner_id = await TradingPartnerRequestAuthorizer.GetTradingPartnerID(authHeader, driverDB);

                        foreach (string epc in query.query.MATCH_anyEPC)
                        {
                            // query the configured url for the epc
                            string url = _configuration.EventURLTemplate.Replace("{epc}", epc).Replace("{account_id}", accountID.ToString()).Replace("{tradingpartner_id}", tradingpartner_id.ToString());
                            using (LimitedPoolItem<HttpClient> item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                            {
                                HttpClient client = item.Value;
                                var response = await client.GetAsync(url);
                                string localData = await response.Content.ReadAsStringAsync();

                                // pass the results through the configured mapper
                                List<ITEEvent> theseEvents = _configuration.Mapper.MapToGS1Events(localData, new Dictionary<string, object>());
                                events.AddRange(theseEvents);
                            }
                        }
                    }
                    else
                    {
                        return new UnauthorizedResult();
                    }
                }

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
