namespace TraceabilityDriver.Services.Authentication
{
    public interface IApiKeyStore
    {
        Task<bool> IsValidKeyAsync(string key);
    }
}
