using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using OpenTraceability.Queries;
using System.Net;
using TraceabilityDriver.Models.Traceback;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="TracebackService"/>.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class TracebackServiceTests
    {
        private Mock<HttpMessageHandler> _mockHandler = null!;
        private TracebackService _tracebackService = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            OpenTraceability.GDST.Setup.Initialize();
        }

        /// <summary>
        /// Builds a service whose HTTP layer answers every request with 404, simulating an external server that resolves nothing.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _mockHandler = new Mock<HttpMessageHandler>();
            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            Mock<IHttpClientFactory> mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_mockHandler.Object));

            _tracebackService = new TracebackService(mockFactory.Object, NullLogger<TracebackService>.Instance);
        }

        /// <summary>
        /// A malformed EPC must be recorded as a per-EPC error without aborting the traceback.
        /// </summary>
        [Test]
        public async Task TracebackAsync_MalformedEpc_RecordsErrorWithoutThrowing()
        {
            // Arrange
            DigitalLinkQueryOptions options = new DigitalLinkQueryOptions { URL = new Uri("https://resolver.example.com/") };

            // Act
            TracebackFetchResult result = await _tracebackService.TracebackAsync(new List<string> { "this-is-not-an-epc" }, options, CancellationToken.None);

            // Assert
            Assert.That(result.Errors, Has.Some.Contains("this-is-not-an-epc"));
            Assert.That(result.Document.Events, Is.Empty);
        }

        /// <summary>
        /// When the digital link resolver cannot resolve an EPC, the error is recorded and the remaining EPCs are still processed.
        /// </summary>
        [Test]
        public async Task TracebackAsync_UnresolvableDigitalLink_RecordsErrorPerEpcAndContinues()
        {
            // Arrange
            DigitalLinkQueryOptions options = new DigitalLinkQueryOptions { URL = new Uri("https://resolver.example.com/") };
            List<string> epcs = new List<string>
            {
                "urn:epc:id:sgtin:0614141.107346.2018",
                "urn:epc:id:sgtin:0614141.107346.2019"
            };

            // Act
            TracebackFetchResult result = await _tracebackService.TracebackAsync(epcs, options, CancellationToken.None);

            // Assert
            Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(2), "Each unresolvable EPC should surface its own error.");
            Assert.That(result.Document.Events, Is.Empty);
        }

        /// <summary>
        /// A null EPC list must be rejected.
        /// </summary>
        [Test]
        public void TracebackAsync_NullEpcs_ThrowsArgumentNullException()
        {
            // Arrange
            DigitalLinkQueryOptions options = new DigitalLinkQueryOptions { URL = new Uri("https://resolver.example.com/") };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _tracebackService.TracebackAsync(null!, options, CancellationToken.None));
        }

        /// <summary>
        /// A canceled token must abort the traceback before any EPC is processed.
        /// </summary>
        [Test]
        public void TracebackAsync_CanceledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            DigitalLinkQueryOptions options = new DigitalLinkQueryOptions { URL = new Uri("https://resolver.example.com/") };
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            Assert.ThrowsAsync<OperationCanceledException>(() => _tracebackService.TracebackAsync(new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" }, options, cts.Token));
        }
    }
}
