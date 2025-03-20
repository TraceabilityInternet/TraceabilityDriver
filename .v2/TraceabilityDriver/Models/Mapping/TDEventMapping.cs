using System.Reflection;
using Newtonsoft.Json.Linq;

namespace TraceabilityDriver.Models.Mapping;

/// <summary>
/// This is the model used to represent the event mapping.
/// </summary>
public class TDEventMapping
{
    /// <summary>
    /// The fields to map.
    /// </summary>
    public List<TDEventMappingField> Fields { get; set; } = new ();

    /// <summary>
    /// The JSON object to map.
    /// </summary>
    public JObject JSON { get; set; } = [];

    /// <summary>
    /// Generates the fields from the JSON object.
    /// </summary>
    /// <param name="errors">The errors that occurred during the generation.</param>
    /// <returns>True if the fields were generated successfully, false otherwise.</returns>
    public bool GenerateFields(out List<string> errors)
    {
        errors = new List<string>();

        try
        {
            GenerateFieldsFromJSON(JSON, errors);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Generates the fields from the JSON object.
    /// </summary>
    /// <param name="json">The JSON object to map.</param>
    /// <param name="errors">The errors that occurred during the generation.</param>
    /// <param name="path">The path to the current property.</param>
    private void GenerateFieldsFromJSON(JObject json, List<string> errors, string path = "")
    {
        foreach (var field in json.Properties())
        {
            string fieldPath = (path + "." + field.Name).Trim('.');

            if (field.Value.Type == JTokenType.Object)
            {
                GenerateFieldsFromJSON((JObject)field.Value, errors, fieldPath);
            }
            else if (field.Value.Type == JTokenType.Array)
            {
                errors.Add($"The field '{fieldPath}' is an array and is not supported yet in the event mapping definition.");
            }
            else
            {
                // Get the property info for the field.
                PropertyInfo propertyInfo;
                try 
                {
                    propertyInfo = GetPropertyInfo(fieldPath);

                    // Add the field to the fields list.
                    Fields.Add(new TDEventMappingField(fieldPath, field.Value.ToString(), propertyInfo));
                }
                catch (Exception ex)
                {
                    errors.Add($"The field '{fieldPath}' was not found in the common event model: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Gets the property info for the given path. If the path cannot be found, it throws an exception.
    /// </summary>
    /// <param name="path">The path to the property.</param>
    /// <returns>The property info for the given path.</returns>
    private static PropertyInfo GetPropertyInfo(string path)
    {
        var properties = path.Trim('.').Split('.');
        var current = typeof(CommonEvent);
        
        PropertyInfo? propertyInfo = null;
        
        foreach (var property in properties)
        {
            propertyInfo = current.GetProperty(property);
            if (propertyInfo == null)
            {
                throw new Exception($"The property '{path}' was not found in the common event model.");
            }

            current = propertyInfo.PropertyType;
        }

        if (propertyInfo == null)
        {
            throw new Exception($"The property '{path}' was not found in the common event model.");
        }

        return propertyInfo;
    }
}