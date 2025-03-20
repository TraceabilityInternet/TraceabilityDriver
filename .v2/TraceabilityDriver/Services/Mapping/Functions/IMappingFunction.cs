namespace TraceabilityDriver.Services.Mapping.Functions;

public interface IMappingFunction
{
    public void Initialize();
    public string? Execute(List<string?> parameters);
}

