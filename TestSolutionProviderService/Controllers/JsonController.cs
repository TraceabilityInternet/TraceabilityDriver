using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestSolutionProviderService.Controllers
{
    [ApiController]
    [Route("json")]
    public class JsonController : ControllerBase
    {
        public JsonController()
        {
            
        }

        [HttpGet]
        [Route("tradeitem/{gtinStr}")]
        public string GetTradeItem()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("location/{glnStr}")]
        public string GetLocation(string glnStr)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("tradingpartner/{pglnStr}")]
        public string GetTradingPartner(string pglnStr)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("events/{epcStr}")]
        public string GetEvents(string epcStr)
        {
            throw new NotImplementedException();
        }
    }
}
