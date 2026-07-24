using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Encodings.Web;
using TraceabilityDriver.Services.Authentication;

namespace TraceabilityDriver.Tests.Services.Authentication
{
    /// <summary>
    /// Unit tests for <see cref="ApiKeyAuthenticationHandler"/>.
    /// </summary>
    /// <remarks>
    /// The key separation requirement is proven here: each scheme carries its own ValidKeys, so a key
    /// from the query scheme must fail against the traceback scheme and vice versa.
    /// </remarks>
    [TestFixture]
    [Category("UnitTest")]
    public class ApiKeyAuthenticationHandlerTests
    {
        private const string QueryKey = "query-key-123";
        private const string TracebackKey = "traceback-key-456";

        /// <summary>
        /// Builds a handler initialized for a scheme with the given valid keys and an incoming request carrying the given key.
        /// </summary>
        private static async Task<ApiKeyAuthenticationHandler> BuildHandlerAsync(string schemeName, List<string> validKeys, string? requestKey)
        {
            ApiKeyAuthenticationOptions options = new ApiKeyAuthenticationOptions { ValidKeys = validKeys };

            Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>> optionsMonitor = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
            optionsMonitor.Setup(x => x.Get(schemeName)).Returns(options);

            ApiKeyAuthenticationHandler handler = new ApiKeyAuthenticationHandler(optionsMonitor.Object, new LoggerFactory(), UrlEncoder.Default);

            DefaultHttpContext context = new DefaultHttpContext();
            if (requestKey != null)
            {
                context.Request.Headers[options.HeaderName] = requestKey;
            }

            await handler.InitializeAsync(new AuthenticationScheme(schemeName, null, typeof(ApiKeyAuthenticationHandler)), context);
            return handler;
        }

        /// <summary>
        /// A key present in the scheme's ValidKeys must authenticate successfully.
        /// </summary>
        [Test]
        public async Task HandleAuthenticateAsync_ValidKeyForScheme_Succeeds()
        {
            // Arrange
            ApiKeyAuthenticationHandler handler = await BuildHandlerAsync("ApiKey", new List<string> { QueryKey }, QueryKey);

            // Act
            AuthenticateResult result = await handler.AuthenticateAsync();

            // Assert
            Assert.That(result.Succeeded, Is.True);
        }

        /// <summary>
        /// A key not in the scheme's ValidKeys must fail.
        /// </summary>
        [Test]
        public async Task HandleAuthenticateAsync_UnknownKey_Fails()
        {
            // Arrange
            ApiKeyAuthenticationHandler handler = await BuildHandlerAsync("ApiKey", new List<string> { QueryKey }, "wrong-key");

            // Act
            AuthenticateResult result = await handler.AuthenticateAsync();

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.Not.Null);
        }

        /// <summary>
        /// A request without any key must produce NoResult, not a failure.
        /// </summary>
        [Test]
        public async Task HandleAuthenticateAsync_MissingKey_ReturnsNoResult()
        {
            // Arrange
            ApiKeyAuthenticationHandler handler = await BuildHandlerAsync("ApiKey", new List<string> { QueryKey }, null);

            // Act
            AuthenticateResult result = await handler.AuthenticateAsync();

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.None, Is.True);
        }

        /// <summary>
        /// A query key must not authenticate against the traceback scheme — this is the key-separation requirement.
        /// </summary>
        [Test]
        public async Task HandleAuthenticateAsync_QueryKeyAgainstTracebackScheme_Fails()
        {
            // Arrange
            ApiKeyAuthenticationHandler handler = await BuildHandlerAsync("TracebackApiKey", new List<string> { TracebackKey }, QueryKey);

            // Act
            AuthenticateResult result = await handler.AuthenticateAsync();

            // Assert
            Assert.That(result.Succeeded, Is.False, "A query key must never satisfy the traceback scheme.");
        }

        /// <summary>
        /// A traceback key must not authenticate against the query scheme.
        /// </summary>
        [Test]
        public async Task HandleAuthenticateAsync_TracebackKeyAgainstQueryScheme_Fails()
        {
            // Arrange
            ApiKeyAuthenticationHandler handler = await BuildHandlerAsync("ApiKey", new List<string> { QueryKey }, TracebackKey);

            // Act
            AuthenticateResult result = await handler.AuthenticateAsync();

            // Assert
            Assert.That(result.Succeeded, Is.False, "A traceback key must never satisfy the query scheme.");
        }

        /// <summary>
        /// A scheme configured with no keys must reject every presented key — the secure default for the traceback scheme.
        /// </summary>
        [Test]
        public async Task HandleAuthenticateAsync_EmptyKeyList_FailsAllKeys()
        {
            // Arrange
            ApiKeyAuthenticationHandler handler = await BuildHandlerAsync("TracebackApiKey", new List<string>(), TracebackKey);

            // Act
            AuthenticateResult result = await handler.AuthenticateAsync();

            // Assert
            Assert.That(result.Succeeded, Is.False);
        }

        /// <summary>
        /// The key may also arrive as a query-string parameter named after the header.
        /// </summary>
        [Test]
        public async Task HandleAuthenticateAsync_KeyInQueryString_Succeeds()
        {
            // Arrange
            ApiKeyAuthenticationOptions options = new ApiKeyAuthenticationOptions { ValidKeys = new List<string> { QueryKey } };
            Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>> optionsMonitor = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
            optionsMonitor.Setup(x => x.Get("ApiKey")).Returns(options);

            ApiKeyAuthenticationHandler handler = new ApiKeyAuthenticationHandler(optionsMonitor.Object, new LoggerFactory(), UrlEncoder.Default);
            DefaultHttpContext context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString($"?{options.HeaderName}={QueryKey}");
            await handler.InitializeAsync(new AuthenticationScheme("ApiKey", null, typeof(ApiKeyAuthenticationHandler)), context);

            // Act
            AuthenticateResult result = await handler.AuthenticateAsync();

            // Assert
            Assert.That(result.Succeeded, Is.True);
        }
    }
}
