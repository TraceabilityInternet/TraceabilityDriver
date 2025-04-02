using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace TraceabilityDriver.Services.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private readonly IApiKeyStore _apiKeyStore;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IApiKeyStore apiKeyStore)
            : base(options, logger, encoder)
        {
            _apiKeyStore = apiKeyStore;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string apiKey = string.Empty;

            if (Request.Headers.TryGetValue(Options.HeaderName, out var k))
            {
                apiKey = k.FirstOrDefault()?.ToString() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // Try and read from the query parameters.
                apiKey = Request.Query[Options.HeaderName].FirstOrDefault()?.ToString() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return AuthenticateResult.NoResult();
            }

            if (!await _apiKeyStore.IsValidKeyAsync(apiKey!))
            {
                return AuthenticateResult.Fail("Invalid API key");
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
