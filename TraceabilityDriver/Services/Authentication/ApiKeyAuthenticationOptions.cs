using Microsoft.AspNetCore.Authentication;

namespace TraceabilityDriver.Services.Authentication
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string HeaderName { get; set; } = "X-API-Key";
        public List<string> ValidKeys { get; set; } = new List<string>();
    }
}
