using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using TraceabilityDriver.Models.Traceback;
using TraceabilityDriver.Services.Queues;

namespace TraceabilityDriver.Tests.Services.Queues
{
    /// <summary>
    /// Unit tests for <see cref="HangfireTracebackQueue"/>.
    /// </summary>
    /// <remarks>
    /// Hangfire's Enqueue is an extension method, so these tests mock the underlying virtual
    /// <see cref="IBackgroundJobClient.Create(Job, IState)"/> that it forwards to.
    /// </remarks>
    [TestFixture]
    [Category("UnitTest")]
    public class HangfireTracebackQueueTests
    {
        private Mock<IBackgroundJobClient> _mockClient = null!;
        private HangfireTracebackQueue _queue = null!;

        /// <summary>
        /// Builds a fresh queue over a mocked Hangfire client before each test.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _mockClient = new Mock<IBackgroundJobClient>();
            _mockClient.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("hangfire-job-1");

            _queue = new HangfireTracebackQueue(_mockClient.Object);
        }

        /// <summary>
        /// The constructor must reject a null client so failures surface at composition time.
        /// </summary>
        [Test]
        public void Constructor_WithNullClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            ArgumentNullException? exception = Assert.Throws<ArgumentNullException>(() => new HangfireTracebackQueue(null!));

            Assert.That(exception!.ParamName, Is.EqualTo("backgroundJobClient"));
        }

        /// <summary>
        /// Enqueueing must create an enqueued Hangfire job that invokes the traceback job with the record id and request.
        /// </summary>
        [Test]
        public async Task EnqueueTracebackAsync_ValidArguments_CreatesEnqueuedTracebackJob()
        {
            // Arrange
            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" }, ResolverUrl = "https://resolver.example.com/" };

            // Act
            await _queue.EnqueueTracebackAsync("507f1f77bcf86cd799439011", request);

            // Assert
            _mockClient.Verify(x => x.Create(
                It.Is<Job>(job => job.Type == typeof(TracebackJob)
                               && job.Method.Name == nameof(TracebackJob.ExecuteAsync)
                               && (string)job.Args[0] == "507f1f77bcf86cd799439011"
                               && ReferenceEquals(job.Args[1], request)),
                It.Is<IState>(state => state is EnqueuedState)), Times.Once);
        }
    }
}
