using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenTraceability;
using OpenTraceability.Models.MasterData;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Controllers
{
    /// <summary>
    /// GS1 Digital Link resolver endpoints (Resolver 1.2 / RFC 9264).
    /// </summary>
    /// <remarks>
    /// Requests that ask for the linkset (an <c>Accept</c> header containing
    /// <c>application/linkset+json</c> or <c>application/json</c>, or <c>?linkType=linkset</c>)
    /// receive the RFC 9264 linkset; every other request is 302-redirected to the target of the
    /// requested link type (the default link when none is given). Identifiers backed by master data
    /// return a 404 when the element is not found in the data cache.
    /// </remarks>
    [Authorize]
    [Route("digitallink")]
    public class DigitalLinkController : ControllerBase
    {
        private readonly IDigitalLinkService _digitalLink;

        /// <summary>
        /// Creates the controller over the digital link service.
        /// </summary>
        /// <param name="digitalLink">The service that builds linksets and resolves redirect targets.</param>
        public DigitalLinkController(IDigitalLinkService digitalLink)
        {
            _digitalLink = digitalLink ?? throw new ArgumentNullException(nameof(digitalLink));
        }

        /// <summary>
        /// Resolves the root resolver query, which serves only the EPCIS query interface link.
        /// </summary>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? linkType)
        {
            return await RespondAsync(null, linkType);
        }

        /// <summary>
        /// Resolves an instance-level EPC (GTIN + serial). The linked master data element is the trade item.
        /// </summary>
        [Route("01/{gtin}/21/{serial}")]
        [HttpGet]
        public async Task<IActionResult> InstanceEPC(string gtin, string serial, [FromQuery] string? linkType)
        {
            return await RespondAsync(gtin, linkType);
        }

        /// <summary>
        /// Resolves a class-level EPC (GTIN + lot). The linked master data element is the trade item.
        /// </summary>
        [HttpGet]
        [Route("01/{gtin}/10/{lot}")]
        public async Task<IActionResult> ClassEPC(string gtin, string lot, [FromQuery] string? linkType)
        {
            return await RespondAsync(gtin, linkType);
        }

        /// <summary>
        /// Resolves an SSCC, which has no master data element and serves only the EPCIS query interface link.
        /// </summary>
        [HttpGet]
        [Route("00/{sscc}")]
        public async Task<IActionResult> SSCC(string sscc, [FromQuery] string? linkType)
        {
            return await RespondAsync(null, linkType);
        }

        /// <summary>
        /// Resolves a GTIN to its trade item master data.
        /// </summary>
        [HttpGet]
        [Route("01/{gtin}")]
        public async Task<IActionResult> GTIN(string gtin, [FromQuery] string? linkType)
        {
            return await RespondAsync(gtin, linkType);
        }

        /// <summary>
        /// Resolves a GLN to its location master data.
        /// </summary>
        [HttpGet]
        [Route("414/{gln}")]
        public async Task<IActionResult> GLN(string gln, [FromQuery] string? linkType)
        {
            return await RespondAsync(gln, linkType);
        }

        /// <summary>
        /// Resolves a PGLN to its trading party master data.
        /// </summary>
        [HttpGet]
        [Route("417/{pgln}")]
        public async Task<IActionResult> PGLN(string pgln, [FromQuery] string? linkType)
        {
            return await RespondAsync(pgln, linkType);
        }

        /// <summary>
        /// Serves the linkset when it was explicitly requested, otherwise redirects to the resolved
        /// link target. A null result from the service means the master data element (or requested
        /// link type) is unavailable and yields a 404.
        /// </summary>
        private async Task<IActionResult> RespondAsync(string? identifier, string? linkType)
        {
            if (WantsLinkset(linkType))
            {
                Linkset? linkset = await _digitalLink.BuildLinksetAsync(identifier, BuildAnchor());
                if (linkset == null)
                {
                    return NotFound();
                }

                // Serialize with Newtonsoft so the linkset's dynamic URI keys (JsonExtensionData) render.
                return Content(JsonConvert.SerializeObject(linkset), DigitalLinkVocab.LinksetMediaType);
            }

            string? target = await _digitalLink.ResolveTargetHrefAsync(identifier, linkType);
            if (target == null)
            {
                return NotFound();
            }

            return Redirect(target + Request.QueryString.Value);
        }

        /// <summary>
        /// The linkset is returned only when explicitly requested, per the resolver standard: the
        /// reserved <c>linkType=linkset</c> value or an <c>Accept</c> header carrying the linkset
        /// (or plain JSON) media type.
        /// </summary>
        private bool WantsLinkset(string? linkType)
        {
            if (!string.IsNullOrWhiteSpace(linkType) && linkType!.ToLower() == DigitalLinkVocab.LinksetLinkType)
            {
                return true;
            }

            return AcceptContains(DigitalLinkVocab.LinksetMediaType) || AcceptContains("application/json");
        }

        /// <summary>
        /// Checks whether the request's Accept header includes the given media type.
        /// </summary>
        private bool AcceptContains(string mediaType)
        {
            foreach (var accept in Request.GetTypedHeaders().Accept)
            {
                if (string.Equals(accept.MediaType.Value, mediaType, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds the anchor URI the linkset items are anchored to: the digital link URI as requested.
        /// </summary>
        private string BuildAnchor()
        {
            return $"{Request.Scheme}://{Request.Host}{Request.Path}";
        }
    }
}
