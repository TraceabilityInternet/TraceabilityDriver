using Newtonsoft.Json.Linq;
using OpenTraceability;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.MasterData;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Default implementation of <see cref="IDigitalLinkService"/> backed by the GDST data cache.
    /// </summary>
    /// <remarks>
    /// Master data existence is checked through <see cref="IDatabaseService.QueryMasterData"/> using
    /// the raw identifier, matching how the <c>/masterdata/{id}</c> endpoint resolves it. The link
    /// relation types come from the shared <see cref="DigitalLinkVocab"/> constants so the driver
    /// and the OpenTraceability resolvers always agree.
    /// </remarks>
    public class DigitalLinkService : IDigitalLinkService
    {
        private readonly IDatabaseService _dbService;
        private readonly ILogger<DigitalLinkService> _logger;
        private readonly string _baseUrl;

        /// <summary>
        /// Creates a new digital link service over the GDST data cache.
        /// </summary>
        /// <param name="dbService">The data cache used to check whether master data elements exist.</param>
        /// <param name="config">The application configuration; the <c>URL</c> value is the public base URL the links point at.</param>
        /// <param name="logger">The logger used for resolution diagnostics.</param>
        public DigitalLinkService(IDatabaseService dbService, IConfiguration config, ILogger<DigitalLinkService> logger)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (config == null) throw new ArgumentNullException(nameof(config));

            _baseUrl = (config["URL"] ?? string.Empty).TrimEnd('/');
        }

        /// <inheritdoc/>
        public async Task<Linkset?> BuildLinksetAsync(string? identifier, string anchor)
        {
            (bool found, string? masterDataHref) = await ResolveMasterDataHrefAsync(identifier);
            if (!found)
            {
                return null;
            }

            string epcisHref = _baseUrl + "/epcis";
            LinksetItem item = new LinksetItem { anchor = anchor };

            // The default link is master data when the identifier has some, otherwise the EPCIS
            // repository. It is described by both gs1:defaultLink and its descriptive link type,
            // per the resolver standard.
            if (masterDataHref != null)
            {
                item.linkTypes[DigitalLinkVocab.DefaultLinkUri] = LinkArray(masterDataHref, "Default Master Data");
                item.linkTypes[DigitalLinkVocab.MasterDataUri] = LinkArray(masterDataHref, "Master Data");
                item.linkTypes[DigitalLinkVocab.EpcisUri] = LinkArray(epcisHref, "EPCIS Repository");
            }
            else
            {
                item.linkTypes[DigitalLinkVocab.DefaultLinkUri] = LinkArray(epcisHref, "Default EPCIS Repository");
                item.linkTypes[DigitalLinkVocab.EpcisUri] = LinkArray(epcisHref, "EPCIS Repository");
            }

            return new Linkset { linkset = new List<LinksetItem> { item } };
        }

        /// <inheritdoc/>
        public async Task<string?> ResolveTargetHrefAsync(string? identifier, string? linkType)
        {
            (bool found, string? masterDataHref) = await ResolveMasterDataHrefAsync(identifier);
            if (!found)
            {
                return null;
            }

            string epcisHref = _baseUrl + "/epcis";

            // No specific type requested: redirect to the default link.
            if (string.IsNullOrWhiteSpace(linkType))
            {
                return masterDataHref ?? epcisHref;
            }

            string normalized = linkType!.ToLower();
            if (normalized == DigitalLinkVocab.EpcisCurie.ToLower() || normalized == DigitalLinkVocab.EpcisUri.ToLower())
            {
                return epcisHref;
            }
            if (normalized == DigitalLinkVocab.MasterDataCurie.ToLower() || normalized == DigitalLinkVocab.MasterDataUri.ToLower())
            {
                return masterDataHref;
            }
            if (normalized == DigitalLinkVocab.DefaultLinkCurie.ToLower() || normalized == DigitalLinkVocab.DefaultLinkUri.ToLower())
            {
                return masterDataHref ?? epcisHref;
            }

            _logger.LogDebug("The requested link type {LinkType} is not served by this resolver.", linkType);
            return null;
        }

        /// <summary>
        /// Resolves the master data href for an identifier. A null identifier is not an error — it
        /// means no master data element applies (found, no href). A non-null identifier that is not
        /// in the data cache reports not found so callers can respond with a 404.
        /// </summary>
        private async Task<(bool found, string? masterDataHref)> ResolveMasterDataHrefAsync(string? identifier)
        {
            if (identifier == null)
            {
                return (true, null);
            }

            IVocabularyElement? element = await _dbService.QueryMasterData(identifier);
            if (element == null)
            {
                _logger.LogDebug("No master data element was found for the identifier {Identifier}.", identifier);
                return (false, null);
            }

            return (true, _baseUrl + "/masterdata/" + identifier);
        }

        /// <summary>
        /// Builds a linkset link relation type value: a JSON array holding a single link object with
        /// the mandatory <c>href</c> and <c>title</c>.
        /// </summary>
        private static JArray LinkArray(string href, string title)
        {
            return new JArray
            {
                new JObject
                {
                    ["href"] = href,
                    ["title"] = title
                }
            };
        }
    }
}
