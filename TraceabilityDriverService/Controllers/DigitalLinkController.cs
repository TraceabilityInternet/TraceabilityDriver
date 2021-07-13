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

        /// <summary>
        /// An HTTP GET request that returns a list of Digital Links associated with a specified GTIN from an Account identified by an account_ID.
        /// </summary>
        /// <param name="account_id"></param>
        /// <param name="gtin"></param>
        /// <param name="linkType"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("gtin/{gtin}")]
        [Route("01/{gtin}")]
        public async Task<IActionResult> GTIN(long account_id, string gtin, [FromQuery] string linkType)
        {
            // validation
            if (string.IsNullOrEmpty(gtin))
            {
                return new BadRequestObjectResult("GTIN is null or empty");
            }

            if(string.IsNullOrEmpty(linkType))
            {
                return new BadRequestObjectResult("Link Type is null or empty");
            }


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

        /// <summary>
        /// An HTTP GET request that returns a list of Digital Links associated with a specified GLN from an Account identified by an account_ID.
        /// </summary>
        /// <param name="account_id"></param>
        /// <param name="gln"></param>
        /// <param name="linkType"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("gln/{gln}")]
        public async Task<IActionResult> GLN(long account_id, string gln, [FromQuery] string linkType)
        {
            // validation
            if (string.IsNullOrEmpty(gln))
            {
                return new BadRequestObjectResult("GLN is null or empty");
            }

            if (string.IsNullOrEmpty(linkType))
            {
                return new BadRequestObjectResult("Link Type is null or empty");
            }



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

        /// <summary>
        /// An HTTP GET request that returns a list of Digital Links associated with a specified PGLN from an Account identified by an account_ID.
        /// </summary>
        /// <param name="account_id"></param>
        /// <param name="pgln"></param>
        /// <param name="linkType"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("pgln/{pgln}")]
        public async Task<IActionResult> PGLN(long account_id, string pgln, [FromQuery] string linkType)
        {
            // validation
            if (string.IsNullOrEmpty(pgln))
            {
                return new BadRequestObjectResult("PGLN is null or empty");
            }

            if (string.IsNullOrEmpty(linkType))
            {
                return new BadRequestObjectResult("Link Type is null or empty");
            }

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

        /// <summary>
        /// An HTTP GET request that returns a list of Digital Links associated with a specified GTIN and lot from an Account identified by an account_ID.
        /// </summary>
        /// <param name="account_id"></param>
        /// <param name="gtin"></param>
        /// <param name="lot"></param>
        /// <param name="linkType"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("gtin/{gtin}/lot/{lot}")]
        public async Task<IActionResult> ClassEPC(long account_id, string gtin, string lot, [FromQuery] string linkType)
        {
            // validation
            if (string.IsNullOrEmpty(gtin))
            {
                return new BadRequestObjectResult("GTIN is null or empty");
            }

            if (string.IsNullOrEmpty(linkType))
            {
                return new BadRequestObjectResult("Link Type is null or empty");
            }


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

        /// <summary>
        /// An HTTP GET request that returns a list of Digital Links associated with a specified GTIN and serial from an Account identified by an account_ID.
        /// </summary>
        /// <param name="account_id"></param>
        /// <param name="gtin"></param>
        /// <param name="serial"></param>
        /// <param name="linkType"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("gtin/{gtin}/serial/{serial}")]
        public async Task<IActionResult> InstanceEPC(long account_id, string gtin, string serial, [FromQuery] string linkType)
        {
            // validation
            if (string.IsNullOrEmpty(gtin))
            {
                return new BadRequestObjectResult("GTIN is null or empty");
            }

            if (string.IsNullOrEmpty(linkType))
            {
                return new BadRequestObjectResult("Link Type is null or empty");
            }

            if (string.IsNullOrEmpty(serial))
            {
                return new BadRequestObjectResult("Serial is null or empty");
            }

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

        /// <summary>
        /// Returns all the Digital Links from the MongoDB.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="linkType"></param>
        /// <returns></returns>
        private async Task<List<ITEDigitalLink>> GetLinks(string identifier, string linkType)
        {
            try
            {
                //// validation How to handle when return type doesn't accept BadRequestObjectResult?

                //List<BadRequestObjectResult> badRequests = new List<BadRequestObjectResult>;
                //if (string.IsNullOrEmpty(identifier))
                //{
                //    badRequests.Add(new BadRequestObjectResult("Identifier is null or empty"));
                //}

                //if (string.IsNullOrEmpty(linkType))
                //{
                //    badRequests.Add(new BadRequestObjectResult("Link Type is null or empty"));
                //}

                //if(badRequests.Count > 0)
                //{
                //    return (List<ITEDigitalLink>)badRequests;
                //}


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
