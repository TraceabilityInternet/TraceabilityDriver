using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver.Services.Mapping
{
    public interface IMappingFunctionFactory
    {
        IMappingFunction Create(string functionName);
    }
}