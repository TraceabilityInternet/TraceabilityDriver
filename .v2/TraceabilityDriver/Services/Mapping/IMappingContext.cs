using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services.Mapping
{
    /// <summary>
    /// Provides context to the current mapping being processed.
    /// </summary>
    public interface IMappingContext
    {
        TDMappingConfiguration? Configuration { get; set; }
    }
}
