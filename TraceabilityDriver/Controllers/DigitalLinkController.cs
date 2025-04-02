using Microsoft.AspNetCore.Mvc;
using OpenTraceability.Models.MasterData;

namespace TraceabilityDriver.Controllers
{
    [Route("digitallink")]
    public class DigitalLinkController : ControllerBase
    {
        public static bool ReturnError = false;

        private IConfiguration _config;
        private string _baseURL = "";

        public DigitalLinkController(IConfiguration config)
        {
            _config = config;
            _baseURL = _config["URL"] ?? "";
        }

        [Route("")]
        [HttpGet]
        public IActionResult Get([FromQuery] string linkType)
        {
            if (ReturnError == true) return new BadRequestResult();

            // we will always return the same epcis url
            DigitalLink link = new DigitalLink()
            {
                link = _baseURL + "/epcis",
                linkType = "gs1:epcis",
                authRequired = true
            };
            return Ok(new List<DigitalLink>() { link });
        }

        [Route("gtin/{gtin}/ser/{serial}")]
        [Route("01/{gtin}/21/{serial}")]
        [HttpGet]
        public IActionResult InstanceEPC(string gtin, string serial, [FromQuery] string linkType)
        {
            if (ReturnError == true) return new BadRequestResult();

            // we will always return the same epcis url
            DigitalLink link = new DigitalLink()
            {
                link = _baseURL + "/epcis",
                linkType = "gs1:epcis",
                authRequired = true
            };
            return Ok(new List<DigitalLink>() { link });
        }

        [HttpGet]
        [Route("01/{gtin}/10/{lot}")]
        [Route("gtin/{gtin}/lot/{lot}")]
        public IActionResult ClassEPC(string gtin, string lot, [FromQuery] string linkType)
        {
            if (ReturnError == true) return new BadRequestResult();

            // we will always return the same epcis url
            DigitalLink link = new DigitalLink()
            {
                link = _baseURL + "/epcis",
                linkType = "gs1:epcis",
                authRequired = true
            };
            return Ok(new List<DigitalLink>() { link });
        }

        [HttpGet]
        [Route("sscc/{sscc}")]
        [Route("00/{sscc}")]
        public IActionResult SSCC(string sscc, [FromQuery] string linkType)
        {
            if (ReturnError == true) return new BadRequestResult();

            // we will always return the same epcis url
            DigitalLink link = new DigitalLink()
            {
                link = _baseURL + "/epcis",
                linkType = "gs1:epcis",
                authRequired = true
            };
            return Ok(new List<DigitalLink>() { link });
        }

        [HttpGet]
        [Route("gtin/{gtin}")]
        [Route("01/{gtin}")]
        public IActionResult GTIN(string gtin, [FromQuery] string linkType)
        {
            if (ReturnError == true) return new BadRequestResult();

            // we will always return the same epcis url
            DigitalLink link = new DigitalLink()
            {
                link = _baseURL + $"/masterdata/{gtin}",
                linkType = "gs1:masterData",
                authRequired = true
            };
            return Ok(new List<DigitalLink>() { link });
        }

        [HttpGet]
        [Route("gln/{gln}")]
        [Route("414/{gln}")]
        public IActionResult GLN(string gln, [FromQuery] string linkType)
        {
            if (ReturnError == true) return new BadRequestResult();

            // we will always return the same epcis url
            DigitalLink link = new DigitalLink()
            {
                link = _baseURL + $"/masterdata/{gln}",
                linkType = "gs1:masterData",
                authRequired = true
            };
            return Ok(new List<DigitalLink>() { link });
        }

        [HttpGet]
        [Route("party/{pgln}")]
        [Route("417/{pgln}")]
        public IActionResult PGLN(string pgln, [FromQuery] string linkType)
        {
            if (ReturnError == true) return new BadRequestResult();

            // we will always return the same epcis url
            DigitalLink link = new DigitalLink()
            {
                link = _baseURL + $"/masterdata/{pgln}",
                linkType = "gs1:masterData",
                authRequired = true
            };

            // we will always return the same epcis url
            DigitalLink link2 = new DigitalLink()
            {
                link = _baseURL + "/epcis",
                linkType = "gs1:epcis",
                authRequired = true
            };

            return Ok((new List<DigitalLink>() { link, link2 }).Where(l => string.IsNullOrEmpty(linkType) || l.linkType.ToLower() == linkType.ToLower()));
        }
    }
}