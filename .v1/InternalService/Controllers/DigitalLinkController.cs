using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.DigitalLink;
using TraceabilityEngine.Models.DigitalLink;
using TraceabilityEngine.Util;

namespace InternalService.Controllers
{
    [Route("{accountID}/digital_link")]
    [ApiController]
    public class DigitalLinkController : ControllerBase
    {
        IConfiguration _configuration;

        public DigitalLinkController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("gtin/{gtin}")]
        [Route("01/{gtin}")]
        public IActionResult GTIN(string accountID, string gtin, [FromQuery] string linkType)
        {
            // find a link by the linkType and the identifier type 'gtin'
            List<ITEDigitalLink> links = GetLinks("gtin", linkType);

            // if not found return a NotFoundResult
            if (links == null || links.Count < 1)
            {
                return new NotFoundResult();
            }
            // else, format the link and return it
            else
            {
                foreach (ITEDigitalLink link in links)
                {
                    link.link = link.link.Replace("{$GTIN}", gtin).Replace("{$AccountID}", accountID);
                }
                return new OkObjectResult(links);
            }
        }

        [HttpGet]
        [Route("gln/{gln}")]
        public IActionResult GLN(string accountID, string gln, [FromQuery] string linkType)
        {
            // find a link by the linkType and the identifier type 'gtin'
            List<ITEDigitalLink> links = GetLinks("gln", linkType);

            // if not found return a NotFoundResult
            if (links == null || links.Count < 1)
            {
                return new NotFoundResult();
            }
            // else, format the link and return it
            else
            {
                foreach (ITEDigitalLink link in links)
                {
                    link.link = link.link.Replace("{$GLN}", gln).Replace("{$AccountID}", accountID);
                }
                return new OkObjectResult(links);
            }
        }

        [HttpGet]
        [Route("pgln/{pgln}")]
        public IActionResult PGLN(string accountID, string pgln, [FromQuery] string linkType)
        {
            // find a link by the linkType and the identifier type 'gtin'
            List<ITEDigitalLink> links = GetLinks("pgln", linkType);

            // if not found return a NotFoundResult
            if (links == null || links.Count < 1)
            {
                return new NotFoundResult();
            }
            // else, format the link and return it
            else
            {
                foreach (ITEDigitalLink link in links)
                {
                    link.link = link.link.Replace("{$PGLN}", pgln).Replace("{$AccountID}", accountID);
                }
                return new OkObjectResult(links);
            }
        }

        private List<ITEDigitalLink> GetLinks(string identifier, string linkType)
        {
            try
            {
                List<ITEDigitalLink> links = new List<ITEDigitalLink>();

                // string digitalLinksURL = _configuration.GetValue<string>("DigitalLinksURL");

                EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
                string jsonStr = loader.ReadString("DigitalLinkService.Util.GS1LinkTypes.json");
                List<TEDigitalLink> allLinks = JsonConvert.DeserializeObject<List<TEDigitalLink>>(jsonStr);
                foreach (TEDigitalLink link in allLinks)
                {
                    if (link.identifier == identifier && link.linkType == linkType)
                    {
                        links.Add(link);
                    }
                }

                return links;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    } 
}
