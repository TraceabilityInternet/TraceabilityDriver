﻿using Newtonsoft.Json;
using System.Reflection;
using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services.Mapping
{
    /// <summary>
    /// Represents a source for local mappings to be found in the executing assembly location under
    /// the "Mappings" folder.
    /// </summary>
    public class LocalMappingSource : IMappingSource
    {
        private readonly ILogger<LocalMappingSource> _logger;

        public LocalMappingSource(ILogger<LocalMappingSource> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes the mapping sources.
        /// </summary>
        public List<TDMappingConfiguration> GetMappings()
        {
            // Get the folder path of the assembly.
            var assemblyFolder = Directory.GetCurrentDirectory(); // we want to reference the root of the container, which is set in the docker file WORKDIR command.
            if (assemblyFolder == null)
            {
                _logger.LogError("The executing assembly folder could not be found.");
                return new List<TDMappingConfiguration>();
            }

            // Get the folder path of the mappings.
            var mappingsFolder = Path.Combine(assemblyFolder, "mappings");

            // Create a list of mappings.
            List<TDMappingConfiguration> mappings = new();

            // Scan the directory.
            if (Directory.Exists(mappingsFolder))
            {
                // Look for all .JSON files in the mappings folder.
                var mappingFiles = Directory.GetFiles(mappingsFolder, "*.json");

                // Load the mappings.
                _logger.LogInformation("Loading mappings from {MappingsFolder}.", mappingsFolder);
                foreach (var mappingFile in mappingFiles)
                {
                    var mappingName = Path.GetFileNameWithoutExtension(mappingFile);
                    _logger.LogInformation("Loading mapping {MappingName} from {MappingFile}.", mappingName, mappingFile);

                    // Load the JSON file.
                    var json = File.ReadAllText(mappingFile);

                    // Deserialize the JSON file.
                    var mapping = JsonConvert.DeserializeObject<TDMappingConfiguration>(json)
                        ?? throw new InvalidOperationException($"The mapping file {mappingFile} could not be deserialized.");

                    mappings.Add(mapping);
                }
            }

#if DEBUG
            string? filePath = Environment.GetEnvironmentVariable("TD_INTEGRATION_TEST_MAPPING_FILE");
            if (!mappings.Any() && !string.IsNullOrEmpty(filePath))
            {
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    var mapping = Newtonsoft.Json.JsonConvert.DeserializeObject<TDMappingConfiguration>(json)
                        ?? throw new InvalidOperationException($"The mapping file {filePath} could not be deserialized.");

                    mappings.Add(mapping);
                }
            }
#endif

            return mappings;
        }
    }
}