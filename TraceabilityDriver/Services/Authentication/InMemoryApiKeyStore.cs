namespace TraceabilityDriver.Services.Authentication
{
    public class InMemoryApiKeyStore : IApiKeyStore
    {
        private readonly List<string> _validKeys = new();

        public InMemoryApiKeyStore(IConfiguration config)
        {
            _validKeys = config.GetSection("ApiKeyAuthentication:ValidKeys").Get<List<string>>() ?? new List<string>();
        }

        public Task<bool> IsValidKeyAsync(string key) => Task.FromResult(_validKeys.Contains(key));
    }
}
