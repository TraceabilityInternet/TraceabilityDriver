using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services.Mapping
{
    /// <summary>
    /// Holds the configuration and mapping that is currently being processed.
    /// </summary>
    public class MappingContext : IMappingContext
    {
        /// <summary>
        /// The current mapping configuration file being processed.
        /// </summary>
        public TDMappingConfiguration? Configuration { get; set; } = null;

        public MappingContext()
        {

        }
    }
}
