using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TraceabilityDriverService.Controllers
{
    public class IndexController
    {
        [HttpGet]
        [Route("")]
        public string Index()
        {
            return "Hello!";
        }
    }
}
