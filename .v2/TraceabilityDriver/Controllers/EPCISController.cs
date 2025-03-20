using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Models.MasterData;
using OpenTraceability.Queries;
using OpenTraceability.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Controllers
{
    [Route("epcis")]
    public class EPCISController : ControllerBase
    {
        private readonly MongoDBService _mongoService;
        private static bool _databaseInitialized = false;

        public EPCISController(MongoDBService mongoService)
        {
            _mongoService = mongoService;
            
            // Initialize database with sample data if not already done
            if (!_databaseInitialized)
            {
                InitializeDatabase().Wait();
                _databaseInitialized = true;
            }
        }

        private async Task InitializeDatabase()
        {
            // Initialize MongoDB with this data
            await _mongoService.InitializeDatabase();
        }

        [Route("queries/SimpleEventQuery")]
        [HttpPost]
        public async Task<IActionResult> SimpleQueryPost([FromBody] EPCISQueryParameters options)
        {
            return await SimpleQuery_internal(options);
        }

        [Route("events")]
        [HttpGet]
        public async Task<IActionResult> SimpleQueryGet()
        {
            string url = this.HttpContext.Request.GetEncodedUrl();
            Uri relativeUri = new Uri(url, UriKind.Absolute);
            EPCISQueryParameters options = new EPCISQueryParameters(relativeUri);

            return await SimpleQuery_internal(options);
        }

        private async Task<IActionResult> SimpleQuery_internal(EPCISQueryParameters options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.query == null) throw new ArgumentNullException(nameof(options.query));
            if (!options.IsValid(out string? error)) throw new ArgumentException($"options are not valid. {error}");

            // Query MongoDB for events
            EPCISQueryDocument epcisDoc = await _mongoService.QueryEvents(options);

            // Determine response format based on headers
            IEPCISQueryDocumentMapper mapper = OpenTraceabilityMappers.EPCISQueryDocument.XML;
            if (HttpContext.Request.Headers["GS1-EPCIS-Version"].FirstOrDefault() != "1.2")
            {
                epcisDoc.EPCISVersion = EPCISVersion.V2;
                mapper = OpenTraceabilityMappers.EPCISQueryDocument.JSON;
            }

            // Add response headers
            foreach (var header in HttpContext.Request.Headers.Where(h => h.Key.StartsWith("GS1-")))
            {
                HttpContext.Response.Headers.Add(header);
            }

            HttpContext.Response.StatusCode = 201;
            await HttpContext.Response.WriteAsync(mapper.Map(epcisDoc));
            return Empty;
        }

        private IActionResult? ValidateHeader(string name, string expectedValue, string expectedValueText)
        {
            if (this.Request.Headers.TryGetValue(name, out StringValues values))
            {
                if (values.Count < 1)
                {
                    return BadRequest($"No '{name}' header found. This should be set to a value of {expectedValueText}");
                }
                if (values.Count > 1)
                {
                    return BadRequest($"Multiple '{name}' HTTP headers on request. Should only have one.");
                }
                if (values[0] != expectedValue)
                {
                    return BadRequest($"The value for the '{name}' is not a valid value. The provided value was '{values[0]}' and the value should be {expectedValueText}");
                }
            }

            return null;
        }

        private IActionResult? ValidateHeader_GreaterThanOrEqual(string name, double expectedValue, string expectedValueText)
        {
            if (this.Request.Headers.TryGetValue(name, out StringValues values))
            {
                if (values.Count < 1)
                {
                    return BadRequest($"No '{name}' header found. This should be set to a value of {expectedValueText}");
                }
                if (values.Count > 1)
                {
                    return BadRequest($"Multiple '{name}' HTTP headers on request. Should only have one.");
                }
                if (!double.TryParse(values[0], out double dbl))
                {
                    return BadRequest($"The value for the '{name}' is not a valid number. The value provided was '{values[0]}'.");
                }
                if (dbl < expectedValue)
                {
                    return BadRequest($"The value for the '{name}' is not a valid value. The provided value was '{values[0]}' and the value should be {expectedValueText}");
                }
            }

            return null;
        }
    }
}