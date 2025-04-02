using System.Text.RegularExpressions;

namespace TraceabilityDriver.Services.Mapping.Functions;

/// <summary>
/// The function will take multiple values and strip
/// all non-alphanumeric characters from each value,
/// and then concatenate the values together.
/// </summary>
public partial class GenerateIdentifierFunction : IMappingFunction
{
    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex StripSpecialCharactersRegex();

    public string Execute(List<string?> parameters)
    {
        return string.Join("-", parameters.Select(ReplaceNonAlphanumericCharacters));
    }

    private static string ReplaceNonAlphanumericCharacters(string? input)
    {
        return StripSpecialCharactersRegex().Replace(input ?? string.Empty, string.Empty);
    }

    public void Initialize()
    {
        // do nothing
    }
}
