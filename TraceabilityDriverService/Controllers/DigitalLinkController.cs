using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.DigitalLink;
using TraceabilityEngine.Models.DigitalLink;
using TraceabilityEngine.Util;

namespace TraceabilityDriverService.Controllers
{
    [Route("{account_id}/digital_link")]
    [ApiController]
    public class DigitalLinkController : ControllerBase
    {
        ITDConfiguration _configuration;

        public DigitalLinkController(ITDConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("gtin/{gtin}")]
        [Route("01/{gtin}")]
        public async Task<IActionResult> GTIN(long account_id, string gtin, [FromQuery] string linkType)
        {
            // find a link by the linkType and the identifier type 'gtin'
            List<ITEDigitalLink> links = await GetLinks("gtin", linkType);

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
                    link.link = link.link.Replace("{gtin}", gtin)
                                    .Replace("{account_id}", account_id.ToString())
                                    .Replace("{url}", _configuration.URL);
                }
                return new OkObjectResult(links);
            }
        }

        [HttpGet]
        [Route("gln/{gln}")]
        public async Task<IActionResult> GLN(long account_id, string gln, [FromQuery] string linkType)
        {
            // find a link by the linkType and the identifier type 'gtin'
            List<ITEDigitalLink> links = await GetLinks ("gln", linkType);

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
                    link.link = link.link.Replace("{gln}", gln)
                                    .Replace("{account_id}", account_id.ToString())
                                    .Replace("{url}", _configuration.URL);
                }
                return new OkObjectResult(links);
            }
        }

        [HttpGet]
        [Route("pgln/{pgln}")]
        public async Task<IActionResult> PGLN(long account_id, string pgln, [FromQuery] string linkType)
        {
            // find a link by the linkType and the identifier type 'gtin'
            List<ITEDigitalLink> links = await GetLinks("pgln", linkType);

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
                    link.link = link.link.Replace("{pgln}", pgln)
                                    .Replace("{account_id}", account_id.ToString())
                                    .Replace("{url}", _configuration.URL);
                }
                return new OkObjectResult(links);
            }
        }

        [HttpGet]
        [Route("gtin/{gtin}/lot/{lot}")]
        public async Task<IActionResult> ClassEPC(long account_id, string gtin, string lot, [FromQuery] string linkType)
        {
            // find a link by the linkType and the identifier type 'gtin'
            List<ITEDigitalLink> links = await GetLinks("epc", linkType);

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
                    link.link = link.link.Replace("{account_id}", account_id.ToString())
                                         .Replace("{url}", _configuration.URL);
                }
                return new OkObjectResult(links);
            }
        }

        [HttpGet]
        [Route("gtin/{gtin}/serial/{serial}")]
        public async Task<IActionResult> InstanceEPC(long account_id, string gtin, string serial, [FromQuery] string linkType)
        {
            // find a link by the linkType and the identifier type 'gtin'
            List<ITEDigitalLink> links = await GetLinks("epc", linkType);

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
                    link.link = link.link.Replace("{account_id}", account_id.ToString())
                                         .Replace("{url}", _configuration.URL);
                }
                return new OkObjectResult(links);
            }
        }

        private async Task<List<ITEDigitalLink>> GetLinks(string identifier, string linkType)
        {
            try
            {
                List<ITEDigitalLink> links = new List<ITEDigitalLink>();

                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    List<ITEDigitalLink> allLinks = await driverDB.LoadDigitalLinks();
                    foreach (TEDigitalLink link in allLinks)
                    {
                        if (link.identifier == identifier && link.linkType == linkType)
                        {
                            links.Add(link);
                        }
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
