using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace TraceabilityDriver.Services.Authentication
{
    public class AlwaysAuthenticatedHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public AlwaysAuthenticatedHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Create a default identity with minimal claims
            var claims = new[] { new Claim(ClaimTypes.Name, "DefaultUser") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            // Return a successful authentication result
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
