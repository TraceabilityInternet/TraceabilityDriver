using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InternalService.Authentication
{
    public class APIKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public APIKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {

        }

#pragma warning disable 1998
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string internalServiceAPIKey = "";

            string authToken = Context.Request.Headers["Authorization"].First().Split(" ").First();
            if (authToken != internalServiceAPIKey)
            {
                ClaimsIdentity identity = new ClaimsIdentity("AuthToken", "AuthToken", "Internal");
                ClaimsPrincipal principal = new ClaimsPrincipal(new List<ClaimsIdentity>() { identity });
                AuthenticationProperties properties = new AuthenticationProperties();
                properties.Items.Add("AuthToken", authToken);
                AuthenticationTicket ticket = new AuthenticationTicket(principal, "APIKeyAuthenticationHandler");
                return AuthenticateResult.Success(ticket);
            }
            else
            {
                return AuthenticateResult.Fail("Failed to verify the authentication token.");
            }
        }
#pragma warning restore 1998
    }
}
