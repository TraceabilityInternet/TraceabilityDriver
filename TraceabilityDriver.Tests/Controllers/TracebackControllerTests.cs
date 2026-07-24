using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TraceabilityDriver.Controllers;
using TraceabilityDriver.Models.Traceback;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Controllers
{
    /// <summary>
    /// Unit tests for <see cref="TracebackController"/>.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class TracebackControllerTests
    {
        private Mock<IIngestionService> _mockIngestionService = null!;
        private Mock<IDatabaseService> _mockDbService = null!;
        private TracebackController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _mockIngestionService = new Mock<IIngestionService>();
            _mockDbService = new Mock<IDatabaseService>();

            _controller = new TracebackController(new Mock<ILogger<TracebackController>>().Object, _mockIngestionService.Object, _mockDbService.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        /// <summary>
        /// A valid request must run the ingestion and return the finalized record.
        /// </summary>
        [Test]
        public async Task ExecuteTraceback_ValidRequest_ReturnsOkWithRecord()
        {
            // Arrange
            TracebackRecord record = new TracebackRecord { Status = TracebackStatus.Completed };
            _mockIngestionService.Setup(x => x.IngestTracebackAsync(It.IsAny<TracebackRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(record);

            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" } };

            // Act
            IActionResult result = await _controller.ExecuteTraceback(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(((OkObjectResult)result).Value, Is.SameAs(record));
            _mockIngestionService.Verify(x => x.IngestTracebackAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// A request without EPCs must be rejected with 400 before the ingestion service is called.
        /// </summary>
        [Test]
        public async Task ExecuteTraceback_EmptyEpcs_ReturnsBadRequest()
        {
            // Arrange
            TracebackRequest request = new TracebackRequest { Epcs = new List<string>() };

            // Act
            IActionResult result = await _controller.ExecuteTraceback(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            _mockIngestionService.Verify(x => x.IngestTracebackAsync(It.IsAny<TracebackRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// A validation failure inside the ingestion service (e.g. no resolver URL) must map to 400.
        /// </summary>
        [Test]
        public async Task ExecuteTraceback_IngestionThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            _mockIngestionService.Setup(x => x.IngestTracebackAsync(It.IsAny<TracebackRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new ArgumentException("No valid resolver URL was provided."));

            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" } };

            // Act
            IActionResult result = await _controller.ExecuteTraceback(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        /// <summary>
        /// An unexpected failure must map to a 500 problem response.
        /// </summary>
        [Test]
        public async Task ExecuteTraceback_IngestionThrowsUnexpectedException_ReturnsProblem()
        {
            // Arrange
            _mockIngestionService.Setup(x => x.IngestTracebackAsync(It.IsAny<TracebackRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Database down."));

            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" } };

            // Act
            IActionResult result = await _controller.ExecuteTraceback(request);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(500));
        }

        /// <summary>
        /// The listing endpoint must pass paging through to the database service.
        /// </summary>
        [Test]
        public async Task GetTracebacks_WithPaging_ReturnsRecords()
        {
            // Arrange
            List<TracebackRecord> records = new List<TracebackRecord> { new TracebackRecord(), new TracebackRecord() };
            _mockDbService.Setup(x => x.GetTracebacksAsync(25, 50)).ReturnsAsync(records);

            // Act
            IActionResult result = await _controller.GetTracebacks(top: 25, skip: 50);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(((OkObjectResult)result).Value, Is.SameAs(records));
        }

        /// <summary>
        /// An unknown traceback id must produce 404.
        /// </summary>
        [Test]
        public async Task GetTraceback_UnknownId_ReturnsNotFound()
        {
            // Arrange
            _mockDbService.Setup(x => x.GetTracebackAsync("missing")).ReturnsAsync((TracebackRecord?)null);

            // Act
            IActionResult result = await _controller.GetTraceback("missing");

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        /// <summary>
        /// A known traceback id must return its record.
        /// </summary>
        [Test]
        public async Task GetTraceback_KnownId_ReturnsRecord()
        {
            // Arrange
            TracebackRecord record = new TracebackRecord();
            _mockDbService.Setup(x => x.GetTracebackAsync(record.Id)).ReturnsAsync(record);

            // Act
            IActionResult result = await _controller.GetTraceback(record.Id);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(((OkObjectResult)result).Value, Is.SameAs(record));
        }

        /// <summary>
        /// The items endpoint must 404 for an unknown traceback and return the ledger for a known one.
        /// </summary>
        [Test]
        public async Task GetTracebackItems_KnownId_ReturnsLedger()
        {
            // Arrange
            TracebackRecord record = new TracebackRecord();
            List<TracebackItem> items = new List<TracebackItem> { new TracebackItem { TracebackId = record.Id } };
            _mockDbService.Setup(x => x.GetTracebackAsync(record.Id)).ReturnsAsync(record);
            _mockDbService.Setup(x => x.GetTracebackItemsAsync(record.Id)).ReturnsAsync(items);

            // Act
            IActionResult result = await _controller.GetTracebackItems(record.Id);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(((OkObjectResult)result).Value, Is.SameAs(items));
        }

        /// <summary>
        /// The items endpoint must 404 when the traceback record does not exist.
        /// </summary>
        [Test]
        public async Task GetTracebackItems_UnknownId_ReturnsNotFound()
        {
            // Arrange
            _mockDbService.Setup(x => x.GetTracebackAsync("missing")).ReturnsAsync((TracebackRecord?)null);

            // Act
            IActionResult result = await _controller.GetTracebackItems("missing");

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
            _mockDbService.Verify(x => x.GetTracebackItemsAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
