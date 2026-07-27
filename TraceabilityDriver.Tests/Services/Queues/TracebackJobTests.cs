using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TraceabilityDriver.Models.Traceback;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Queues;

namespace TraceabilityDriver.Tests.Services.Queues
{
    /// <summary>
    /// Unit tests for <see cref="TracebackJob"/>.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class TracebackJobTests
    {
        private Mock<IIngestionService> _mockIngestionService = null!;
        private TracebackJob _job = null!;

        /// <summary>
        /// Builds a fresh job over a mocked ingestion service before each test.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _mockIngestionService = new Mock<IIngestionService>();
            _job = new TracebackJob(_mockIngestionService.Object, NullLogger<TracebackJob>.Instance);
        }

        /// <summary>
        /// The constructor must reject a null ingestion service so failures surface at composition time.
        /// </summary>
        [Test]
        public void Constructor_WithNullIngestionService_ThrowsArgumentNullException()
        {
            // Act & Assert
            ArgumentNullException? exception = Assert.Throws<ArgumentNullException>(() => new TracebackJob(null!, NullLogger<TracebackJob>.Instance));

            Assert.That(exception!.ParamName, Is.EqualTo("ingestionService"));
        }

        /// <summary>
        /// The job must delegate to the ingestion service with the exact record id, request, and token it was given.
        /// </summary>
        [Test]
        public async Task ExecuteAsync_ValidArguments_DelegatesToIngestionService()
        {
            // Arrange
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" }, ResolverUrl = "https://resolver.example.com/" };

            // Act
            await _job.ExecuteAsync("507f1f77bcf86cd799439011", request, tokenSource.Token);

            // Assert
            _mockIngestionService.Verify(x => x.IngestTracebackAsync("507f1f77bcf86cd799439011", request, tokenSource.Token), Times.Once);
            _mockIngestionService.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The job must not swallow ingestion failures — the exception has to reach the queue backend to trigger its retry.
        /// </summary>
        [Test]
        public void ExecuteAsync_IngestionThrows_PropagatesException()
        {
            // Arrange
            _mockIngestionService.Setup(x => x.IngestTracebackAsync(It.IsAny<string>(), It.IsAny<TracebackRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("The external server is unreachable."));

            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" }, ResolverUrl = "https://resolver.example.com/" };

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(() => _job.ExecuteAsync("507f1f77bcf86cd799439011", request, CancellationToken.None));
        }
    }
}
