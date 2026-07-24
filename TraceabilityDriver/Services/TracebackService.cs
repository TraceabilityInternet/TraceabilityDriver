using OpenTraceability.GDST;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Queries;
using TraceabilityDriver.Models.Traceback;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Executes tracebacks against an external traceability server using the OpenTraceability helpers.
    /// </summary>
    /// <remarks>
    /// For each EPC the flow is: discover the EPCIS query interface URL through the digital link resolver,
    /// run the traceback traversal against that interface, and merge the results into one document. GDST
    /// master data (trade items, locations, trading parties with GDST KDEs) is then resolved for the merged
    /// document. The API key is carried on the options objects only — the resolvers apply it per request —
    /// so the pooled HttpClient never accumulates default headers across runs.
    /// </remarks>
    public class TracebackService : ITracebackService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TracebackService> _logger;

        /// <summary>
        /// Creates a new traceback service.
        /// </summary>
        /// <param name="httpClientFactory">Factory for the HTTP clients used to call the external server.</param>
        /// <param name="logger">The logger used for traceback diagnostics.</param>
        public TracebackService(IHttpClientFactory httpClientFactory, ILogger<TracebackService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<TracebackFetchResult> TracebackAsync(List<string> epcs, DigitalLinkQueryOptions resolverOptions, CancellationToken cancellationToken)
        {
            if (epcs == null) throw new ArgumentNullException(nameof(epcs));
            if (resolverOptions == null) throw new ArgumentNullException(nameof(resolverOptions));

            TracebackFetchResult result = new TracebackFetchResult();
            result.Document.EPCISVersion = EPCISVersion.V2;

            HttpClient client = _httpClientFactory.CreateClient();

            _logger.LogDebug("Starting traceback against {ResolverUrl} for {EpcCount} EPC(s).", resolverOptions.URL, epcs.Count);

            // Each EPC is traced independently; a failure on one EPC is recorded and the rest continue.
            foreach (string epcStr in epcs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    EPC epc = new EPC(epcStr);

                    Uri? epcisUrl = await EPCISTraceabilityResolver.GetEPCISQueryInterfaceURL(resolverOptions, epc, client);
                    if (epcisUrl == null)
                    {
                        result.Errors.Add($"Could not resolve the EPCIS query interface URL for {epcStr}.");
                        continue;
                    }

                    EPCISQueryInterfaceOptions queryOptions = new EPCISQueryInterfaceOptions
                    {
                        URL = epcisUrl,
                        APIKey = resolverOptions.APIKey,
                        Format = resolverOptions.Format,
                        Version = resolverOptions.Version,
                        Headers = resolverOptions.Headers
                    };

                    EPCISQueryResults results = await EPCISTraceabilityResolver.Traceback(queryOptions, epc, client);
                    if (results.Document != null)
                    {
                        result.Document.Merge(results.Document);
                    }

                    foreach (var error in results.Errors)
                    {
                        result.Errors.Add($"{epcStr}: {error.Details ?? error.Type.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "The traceback for EPC {EPC} failed and was skipped.", epcStr);
                    result.Errors.Add($"{epcStr}: {ex.Message}");
                }
            }

            // Resolve GDST master data for everything discovered. A failure here still leaves the events usable.
            try
            {
                await GDSTMasterDataResolver.ResolveGDSTMasterData(resolverOptions, result.Document, client);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Master data resolution failed for the traceback against {ResolverUrl}.", resolverOptions.URL);
                result.Errors.Add($"Master data resolution: {ex.Message}");
            }

            // The traversal can return the same event from multiple EPC queries; dedupe by event id so
            // downstream storage and the ledger see each event exactly once.
            result.Document.Events = result.Document.Events.GroupBy(e => e.EventID.ToString()).Select(g => g.First()).ToList();

            _logger.LogInformation("Traceback against {ResolverUrl} returned {EventCount} event(s) and {MasterDataCount} master data element(s) with {ErrorCount} error(s).", resolverOptions.URL, result.Document.Events.Count, result.Document.MasterData.Count, result.Errors.Count);

            return result;
        }
    }
}
