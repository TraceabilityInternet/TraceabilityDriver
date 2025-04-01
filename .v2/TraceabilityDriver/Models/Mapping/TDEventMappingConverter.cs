using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace TraceabilityDriver.Models.Mapping
{
    /// <summary>
    /// Custom JSON converter for TDEventMapping that handles the JSON property and field generation.
    /// </summary>
    public class TDEventMappingConverter : JsonConverter<TDEventMapping>
    {
        /// <summary>
        /// Reads the JSON representation of the TDEventMapping.
        /// </summary>
        /// <param name="reader">The JsonReader to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="hasExistingValue">True if there is an existing value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The deserialized TDEventMapping object.</returns>
        public override TDEventMapping ReadJson(JsonReader reader, Type objectType, TDEventMapping? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Create a new instance or use the existing one
            TDEventMapping mapping = existingValue ?? new TDEventMapping();
            
            // Parse the JSON to a JObject
            JObject jsonObject = JObject.Load(reader);
            
            // Set the JSON property
            mapping.JSON = jsonObject;
            
            // Generate fields from the JSON
            if (!mapping.GenerateFields(out var errors) && errors.Count > 0)
            {
                throw new JsonSerializationException($"Error generating event mapping fields: {string.Join(", ", errors)}");
            }
            
            return mapping;
        }

        /// <summary>
        /// Writes the JSON representation of the TDEventMapping.
        /// </summary>
        /// <param name="writer">The JsonWriter to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, TDEventMapping? value, JsonSerializer serializer)
        {
            if (value?.JSON != null)
            {
                // Simply write out the JSON property
                value.JSON.WriteTo(writer);
            }
            else
            {
                // Write an empty object if JSON is null
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
        }
    }
}
