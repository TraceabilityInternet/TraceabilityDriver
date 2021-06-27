using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Service.Util;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.ObjectPooling;
using TraceabilityEngine.Util.Security;

namespace InternalService.Controllers
{
    [ApiController]
    [Route("{accountID}/epcis/epcs")]
    public class EPCISQueryController : ControllerBase
    {
        private IConfiguration _configuration;
        private string _connectionString;
        private string _urlTemplate;
        private ITETraceabilityDriver _driver;
        bool _requiresAuthorization;

        public EPCISQueryController(IConfiguration config)
        {
            _configuration = config;
            _urlTemplate = _configuration.GetValue<string>("URLTemplate");
            _connectionString = _configuration.GetValue<string>("ConnectionString");
            _requiresAuthorization = _configuration.GetValue<bool>("RequiresAuthorization");

            string dllPath = _configuration.GetValue<string>("DriverDLLPath");
            string className = _configuration.GetValue<string>("DriverClassName");
            _driver = DriverUtil.Load(dllPath, className);
        }

        [HttpGet]
        [Route("{epc}")]
        public async Task<IActionResult> GetEvents(string epc)
        {
            try
            {
                using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
                {
                    string authHeader = Request?.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (!_requiresAuthorization || await TradingPartnerRequestAuthorizer.Authorize(authHeader, epc, driverDB))
                    {
                        // query the configured url for the epc
                        string url = _urlTemplate.Replace("{epc}", epc);
                        using (LimitedPoolItem<HttpClient> item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                        {
                            HttpClient client = item.Value;
                            var response = await client.GetAsync(url);
                            string localData = await response.Content.ReadAsStringAsync();

                            // pass the results through the configured mapper
                            ITEEventMapper mapper = new EPCISJsonMapper_2_0();
                            List<ITEEvent> events = _driver.MapToGS1Events(localData, new Dictionary<string, object>());
                            string gs1Events = mapper.ConvertFromEvents(events);
                            return new OkObjectResult(gs1Events);
                        }
                    }
                    else
                    {
                        return new UnauthorizedResult();
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
