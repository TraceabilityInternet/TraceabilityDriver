using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceabilityDriver.Models.Traceback;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Controllers
{
    /// <summary>
    /// Triggers tracebacks against an external traceability server and exposes the traceback history.
    /// </summary>
    /// <remarks>
    /// This controller authenticates against the "TracebackApiKey" scheme, which carries its own key set
    /// (Authentication:TracebackAPIKey:ValidKeys). Query API keys are deliberately unable to reach these
    /// endpoints, so a leaked query key can never be used to pull external data into the cache.
    /// </remarks>
    [Authorize(AuthenticationSchemes = "TracebackApiKey", Policy = "TracebackApiKey")]
    [Route("traceback")]
    public class TracebackController : ControllerBase
    {
        private readonly ILogger<TracebackController> _logger;
        private readonly IIngestionService _ingestionService;
        private readonly IDatabaseService _dbService;

        /// <summary>
        /// Creates a new traceback controller.
        /// </summary>
        /// <param name="logger">The logger used for request diagnostics.</param>
        /// <param name="ingestionService">The service that executes and records tracebacks.</param>
        /// <param name="dbService">The data cache, used to serve the traceback history.</param>
        public TracebackController(ILogger<TracebackController> logger, IIngestionService ingestionService, IDatabaseService dbService)
        {
            _logger = logger;
            _ingestionService = ingestionService;
            _dbService = dbService;
        }

        /// <summary>
        /// Executes a traceback and ingests the results into the data cache.
        /// </summary>
        /// <param name="request">The EPCs to trace and optional overrides for the external server URL and API key.</param>
        /// <returns>The finalized traceback record, including counts of created/updated resources and any errors.</returns>
        [HttpPost]
        public async Task<IActionResult> ExecuteTraceback([FromBody] TracebackRequest request)
        {
            try
            {
                if (request == null || request.Epcs == null || !request.Epcs.Any(e => !string.IsNullOrWhiteSpace(e)))
                {
                    return BadRequest("At least one EPC is required.");
                }

                TracebackRecord record = await _ingestionService.IngestTracebackAsync(request, HttpContext.RequestAborted);
                return Ok(record);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute the traceback request.");
                return Problem("Failed to execute the traceback request.");
            }
        }

        /// <summary>
        /// Returns traceback records, newest first.
        /// </summary>
        /// <param name="top">The maximum number of records to return.</param>
        /// <param name="skip">The number of records to skip, for paging.</param>
        [HttpGet]
        public async Task<IActionResult> GetTracebacks([FromQuery] int top = 100, [FromQuery] int skip = 0)
        {
            try
            {
                List<TracebackRecord> records = await _dbService.GetTracebacksAsync(top, skip);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query the traceback records.");
                return Problem("Failed to query the traceback records.");
            }
        }

        /// <summary>
        /// Returns a single traceback record by id.
        /// </summary>
        /// <param name="id">The id of the traceback record.</param>
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetTraceback(string id)
        {
            try
            {
                TracebackRecord? record = await _dbService.GetTracebackAsync(id);
                if (record == null)
                {
                    return NotFound();
                }

                return Ok(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query the traceback record {TracebackId}.", id);
                return Problem("Failed to query the traceback record.");
            }
        }

        /// <summary>
        /// Returns the ledger of events and master data a traceback created or updated.
        /// </summary>
        /// <param name="id">The id of the traceback record.</param>
        [HttpGet]
        [Route("{id}/items")]
        public async Task<IActionResult> GetTracebackItems(string id)
        {
            try
            {
                TracebackRecord? record = await _dbService.GetTracebackAsync(id);
                if (record == null)
                {
                    return NotFound();
                }

                List<TracebackItem> items = await _dbService.GetTracebackItemsAsync(id);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query the traceback items for {TracebackId}.", id);
                return Problem("Failed to query the traceback items.");
            }
        }
    }
}
