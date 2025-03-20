using System.Globalization;
using System.Reflection;
using CsvHelper;

namespace TraceabilityDriver.Services.Mapping.Functions;

/// <summary>
/// A mapping function that looks up a value in a dictionary.
/// </summary>
public class DictionaryMappingFunction : IMappingFunction
{
    private readonly ILogger<DictionaryMappingFunction> _logger;
    private readonly Dictionary<string, DictionarySource> _dictionarySources = new ();

    public DictionaryMappingFunction(ILogger<DictionaryMappingFunction> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the dictionary sources.
    /// </summary>
    public void Initialize()
    {
        // Get the folder path of the assembly.
        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (assemblyFolder == null)
        {
            _logger.LogError("The executing assembly folder could not be found.");
            return;
        }

        // Get the folder path of the dictionaries.
        var dictionariesFolder = Path.Combine(assemblyFolder, "Dictionaries");
        
        // Look for all .CSV files in the dictionaries folder.
        var dictionaryFiles = Directory.GetFiles(dictionariesFolder, "*.csv");

        // Load the dictionaries.
        _logger.LogInformation("Loading dictionaries from {DictionariesFolder}.", dictionariesFolder);
        foreach (var dictionaryFile in dictionaryFiles)
        {
            var dictionaryName = Path.GetFileNameWithoutExtension(dictionaryFile);

            _logger.LogInformation("Loading dictionary {DictionaryName} from {DictionaryFile}.", dictionaryName, dictionaryFile);

            _dictionarySources.Add(dictionaryName, new DictionarySource() { DictionaryName = dictionaryName, FilePath = dictionaryFile });

            // Load the CSV file and verify there are two columns.
            using var stream = File.OpenRead(dictionaryFile);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            // Read the header row.
            var header = csv.ReadHeader();

            // Read the rest of the file.
            while (csv.Read())
            {
                string? key = csv.GetField<string>(0);
                string? value = csv.GetField<string>(1);

                // Try to add the key and value to the dictionary.
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    if (!_dictionarySources[dictionaryName].Values.TryAdd(key, value))
                    {
                        _logger.LogError("The key {Key} already exists in the dictionary {DictionaryName}.", key, dictionaryName);
                    }
                }
            }

            _logger.LogInformation("Dictionary {DictionaryName} loaded with {Count} values.", dictionaryName, _dictionarySources[dictionaryName].Values.Count);
        }
    }

    /// <summary>
    /// Executes the function.
    /// </summary>
    /// <param name="parameters">The parameters of the function.</param>
    /// <returns>The return value of the function in a string format.</returns>
    public string? Execute(List<string?> parameters)
    {
        throw new NotImplementedException();
    }
}

public class DictionarySource
{
    public string? DictionaryName { get; set; }
    public string? FilePath { get; set; }
    public Dictionary<string, string> Values { get; set; } = new ();
}
