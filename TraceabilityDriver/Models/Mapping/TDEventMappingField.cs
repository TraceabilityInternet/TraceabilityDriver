using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace TraceabilityDriver.Models.Mapping;

/// <summary>
/// A struct that contains the information about a field in the TDEventMapping.
/// </summary>
public partial class TDEventMappingField
{
    // Define the regex pattern with GeneratedRegexAttribute for compile-time generation
    [GeneratedRegex(@"\(([^)]*)\)")]
    private static partial Regex ArgumentsRegex();

    public TDEventMappingField(string path, string mapping, PropertyInfo propertyInfo)
    {
        Path = path;
        Mapping = mapping;
        PropertyInfo = propertyInfo;
    }

    /// <summary>
    /// The path to the property in the target common event object.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The mapping to use for the field. The mapping can be on of the following:
    /// 
    /// **Static Value** : A static value to be used for the field and is indicated with a ! in the mapping string such as "!123".
    /// **Variable Value** : The value of a column in the data row and is indicated with a $ in the mapping string such as "$Column1".
    /// **Function Call** : A function call to be used for the field and is formatted like a function call in the mapping string such as "Function($123, 22.2)".
    /// </summary>
    public string Mapping { get; set; } = string.Empty;

    /// <summary>
    /// The property info of the field in the target object.
    /// </summary>
    public PropertyInfo PropertyInfo { get; set; } = null!;

    /// <summary>
    /// Analyzes the JSON mapping and returns the values needed to calculate the field
    /// the field value.
    /// </summary>
    public TDEventMappingValue GetMappingValue(DataRow row)
    {
        List<string?> values = new List<string?>();

        if (Mapping.StartsWith('!'))
        {
            values.Add(Mapping.Substring(1));
            return new TDEventMappingValue(Mapping, values, TDEventMappingValueType.Static);
        }
        else if (Mapping.StartsWith('$'))
        {
            values.Add(GetValueFromRow(row, Mapping));
            return new TDEventMappingValue(Mapping, values, TDEventMappingValueType.Variable);
        }
        else
        {
            // Use regex to parse out the arguments.
            var match = ArgumentsRegex().Match(Mapping);
            if (match.Success)
            {
                var arguments = match.Groups[1].Value.Split(',').Select(a => a.Trim()).ToList();

                foreach (var argument in arguments)
                {
                    if (argument.StartsWith('$'))
                    {
                        values.Add(GetValueFromRow(row, argument));
                    }
                    else
                    {
                        values.Add(argument);
                    }
                }
            }

            var functionName = Mapping.Substring(0, match.Index);

            return new TDEventMappingValue(Mapping, values, TDEventMappingValueType.Function, functionName);
        }
    }

    /// <summary>
    /// Gets a value from the data row.
    /// </summary>
    /// <param name="row">The data row to get the value from.</param>
    /// <param name="field">The field to get the value from.</param>
    /// <returns>The value from the data row.</returns>
    public string? GetValueFromRow(DataRow row, string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return null;
        }

        if (field.StartsWith('$'))
        {
            field = field.Substring(1);
        }
        
        if (row.Table.Columns.Contains(field))
        {
            if (row[field] == DBNull.Value)
            {
                return null;
            }

            return row[field].ToString();
        }

        return null;
    }
}
