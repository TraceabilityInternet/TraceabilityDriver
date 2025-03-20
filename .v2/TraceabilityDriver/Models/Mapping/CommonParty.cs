using OpenTraceability.Models.Identifiers;

namespace TraceabilityDriver.Models.Mapping
{
    /// <summary>
    /// Represents a common party entity that inherits from CommonBaseModel. It serves as a base class for party-related
    /// data.
    /// </summary>
    public class CommonParty : CommonBaseModel
    {
        public string? OwnerId { get; set; } = null;

        /// <summary>
        /// Represents the name of an entity as a string.
        /// </summary>
        public string? Name { get; set; } = null;

        /// <summary>
        /// Retrieves a PGLN object using the OwnerId property. Throws an exception if OwnerId is null or whitespace.
        /// </summary>
        /// <returns>Returns a new PGLN instance initialized with the OwnerId.</returns>
        public PGLN GetPGLN()
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(this.OwnerId);
            return new PGLN(this.OwnerId);
        }
    }
}
