using System.Collections.ObjectModel;

namespace TraceabilityDriver.Models.Mapping;

public enum TDEventMappingValueType
{
    Static,
    Variable,
    Function
}

public struct TDEventMappingValue
{
    public string Mapping { get; private set; } = string.Empty;

    public string FunctionName { get; private set; } = string.Empty;

    public List<string?> Values { get; private set; } = new List<string?>();

    public TDEventMappingValueType Type { get; private set; }

    public TDEventMappingValue(string mapping, List<string?> values, TDEventMappingValueType type, string functionName = "")
    {
        Mapping = mapping;
        Values = values;
        Type = type;
        FunctionName = functionName;
    }
}

