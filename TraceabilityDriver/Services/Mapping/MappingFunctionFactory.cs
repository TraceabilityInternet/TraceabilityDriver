using System.Collections.Concurrent;
using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver.Services.Mapping;

/// <summary>
/// A factory for creating mapping functions.
/// </summary>
public class MappingFunctionFactory : IMappingFunctionFactory
{
    private readonly ILogger<MappingFunctionFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, IMappingFunction> _mappingFunctions = new();

    public MappingFunctionFactory(IServiceProvider serviceProvider, ILogger<MappingFunctionFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Creates a mapping function.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    public IMappingFunction Create(string functionName)
    {
        functionName = functionName.Trim().ToLower();

        if (_mappingFunctions.TryGetValue(functionName, out var function))
        {
            return function;
        }

        lock (_lock)
        {
            if (_mappingFunctions.TryGetValue(functionName, out function))
            {
                return function;
            }

            function = Construct(functionName);
            _mappingFunctions.TryAdd(functionName, function);
            return function;
        }
    }

    /// <summary>
    /// Constructs a mapping function.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    private IMappingFunction Construct(string functionName)
    {
        var function = _serviceProvider.GetKeyedService<IMappingFunction>(functionName);

        if (function == null)
        {
            _logger.LogError("The mapping function {FunctionName} is not registered with the service provider.", functionName);
            throw new NotImplementedException($"The mapping function {functionName} is not implemented.");
        }

        function.Initialize();

        return function;
    }
}
