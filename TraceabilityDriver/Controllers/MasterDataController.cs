using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenTraceability.Mappers;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Controllers
{
    [Authorize]
    [Route("masterdata")]
    public class MasterDataController : ControllerBase
    {
        private readonly ILogger<MasterDataController> _logger;
        private readonly IDatabaseService _dbService;

        public MasterDataController(ILogger<MasterDataController> logger, IDatabaseService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetMasterData(string id)
        {
            try
            {
                var masterDataItem = await _dbService.QueryMasterData(id);

                if (masterDataItem != null)
                {
                    masterDataItem.Context = JObject.Parse("{\r\n        \"cbvmda\": \"urn:epcglobal:cbv:mda\",\r\n        \"xsd\": \"http://www.w3.org/2001/XMLSchema#\",\r\n        \"gs1\": \"http://gs1.org/voc/\",\r\n        \"@vocab\": \"http://gs1.org/voc/\",\r\n        \"gdst\": \"https://traceability-dialogue.org/vocab\"\r\n    }");
                    return Ok(OpenTraceabilityMappers.MasterData.GS1WebVocab.Map(masterDataItem));
                }

                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}