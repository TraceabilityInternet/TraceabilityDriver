using System.Globalization;
using System.Reflection;
using CsvHelper;

namespace TraceabilityDriver.Services.Mapping.Functions;

/// <summary>
/// A mapping function that looks up a value in a dictionary.
/// </summary>
public class DictionaryMappingFunction : IMappingFunction
{
    private readonly IMappingContext _context;
    private readonly ILogger<DictionaryMappingFunction> _logger;

    public DictionaryMappingFunction(IMappingContext context, ILogger<DictionaryMappingFunction> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Executes the function.
    /// </summary>
    /// <param name="parameters">The parameters of the function.</param>
    /// <returns>The return value of the function in a string format.</returns>
    public string? Execute(List<string?> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.Count == 2)
        {
            string? value = parameters[0];
            string? dictionaryName = parameters[1];

            if (!string.IsNullOrWhiteSpace(dictionaryName))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (_context.Configuration != null)
                    {
                        if (_context.Configuration.Dictionaries.ContainsKey(dictionaryName))
                        {
                            if (_context.Configuration.Dictionaries[dictionaryName].ContainsKey(value))
                            {
                                return _context.Configuration.Dictionaries[dictionaryName][value];
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    public void Initialize()
    {
        // do nothing
    }
}
