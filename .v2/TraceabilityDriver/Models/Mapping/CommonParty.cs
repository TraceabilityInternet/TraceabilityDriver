using OpenTraceability.Models.Identifiers;

namespace TraceabilityDriver.Models.Mapping
{
    /// <summary>
    /// Represents a common party entity that inherits from CommonBaseModel. It serves as a base class for party-related
    /// data.
    /// </summary>
    public class CommonParty : CommonBaseModel
    {
        /// <summary>
        /// Represents the unique identifier of an entity as a string.
        /// </summary>
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
            return GeneratePGLN(this.OwnerId);
        }

        /// <summary>
        /// Merges data from another CommonParty object into this instance.
        /// Only null or empty properties in the current instance will be updated with non-null values from the source.
        /// </summary>
        /// <param name="source">The source CommonParty object to merge data from.</param>
        /// <exception cref="ArgumentNullException">Thrown if the source parameter is null.</exception>
        public void Merge(CommonParty source)
        {
            ArgumentNullException.ThrowIfNull(source);

            // Only update OwnerId if current value is null or empty
            if (string.IsNullOrWhiteSpace(this.OwnerId) && !string.IsNullOrWhiteSpace(source.OwnerId))
            {
                this.OwnerId = source.OwnerId;
            }

            // Only update Name if current value is null or empty
            if (string.IsNullOrWhiteSpace(this.Name) && !string.IsNullOrWhiteSpace(source.Name))
            {
                this.Name = source.Name;
            }
        }
    }
}
