using OpenTraceability.Models.MasterData;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Builds GS1 Digital Link resolver responses (Resolver 1.2 / RFC 9264) for the identifiers
    /// served by the driver's digital link endpoints.
    /// </summary>
    /// <remarks>
    /// A null identifier means the request has no associated master data element (e.g. an SSCC or
    /// the root resolver query) and only EPCIS links are served. A non-null identifier is looked up
    /// in the GDST data cache; when the master data element is not found the methods return null so
    /// the caller can respond with a 404.
    /// </remarks>
    public interface IDigitalLinkService
    {
        /// <summary>
        /// Builds the RFC 9264 linkset for an identifier.
        /// </summary>
        /// <param name="identifier">The master data identifier (GTIN/GLN/PGLN), or null when the request has no master data element.</param>
        /// <param name="anchor">The digital link URI the linkset items are anchored to.</param>
        /// <returns>The linkset, or null when the master data element is not found.</returns>
        Task<Linkset?> BuildLinksetAsync(string? identifier, string anchor);

        /// <summary>
        /// Resolves the single target href to redirect to for the requested link type. When
        /// <paramref name="linkType"/> is null or empty the default link is returned (master data
        /// when available, otherwise the EPCIS query interface).
        /// </summary>
        /// <param name="identifier">The master data identifier (GTIN/GLN/PGLN), or null when the request has no master data element.</param>
        /// <param name="linkType">The requested link type (compact CURIE or full URI), or null for the default link.</param>
        /// <returns>The target href, or null when the master data element is not found or the requested link type is unavailable.</returns>
        Task<string?> ResolveTargetHrefAsync(string? identifier, string? linkType);
    }
}
