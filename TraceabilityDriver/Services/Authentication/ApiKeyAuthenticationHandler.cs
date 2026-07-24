using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace TraceabilityDriver.Services.Authentication
{
    /// <summary>
    /// Authenticates requests by matching an API key from a header (or query parameter) against the scheme's configured keys.
    /// </summary>
    /// <remarks>
    /// Keys live on <see cref="ApiKeyAuthenticationOptions.ValidKeys"/>, so each registered scheme carries its own
    /// independent key set. This is what keeps the query API keys and the traceback API keys separate: the "ApiKey"
    /// scheme only knows the query keys and the "TracebackApiKey" scheme only knows the traceback keys, so a key
    /// from one scheme can never authenticate against the other.
    /// </remarks>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// <summary>
        /// Reads the API key from the configured header (falling back to a query parameter of the same name) and validates it against the scheme's key list.
        /// </summary>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
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
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!Options.ValidKeys.Contains(apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
