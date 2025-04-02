using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services.Mapping
{
    /// <summary>
    /// Provides the mapping configuration to the synchronize service.
    /// </summary>
    public interface IMappingSource
    {
        /// <summary>
        /// Retrieves a list of TDMappingConfiguration objects.
        /// </summary>
        /// <returns>Returns a List containing TDMappingConfiguration instances.</returns>
        List<TDMappingConfiguration> GetMappings();
    }
}
