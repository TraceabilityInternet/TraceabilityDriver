using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Models.Traceback;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="IngestionService"/>.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class IngestionServiceTests
    {
        private Mock<ITracebackService> _mockTracebackService = null!;
        private Mock<IDatabaseService> _mockDbService = null!;
        private IngestionService _ingestionService = null!;
        private EPCISDocument _testDocument = null!;

        /// <summary>
        /// Loads a real EPCIS document once so tests can hand realistic events and master data to the mocks.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            OpenTraceability.GDST.Setup.Initialize();

            string testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "testdata001.json");
            _testDocument = OpenTraceabilityMappers.EPCISDocument.JSON.Map(File.ReadAllText(testDataPath));
        }

        /// <summary>
        /// Builds a fresh service over mocked dependencies before each test.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _mockTracebackService = new Mock<ITracebackService>();
            _mockDbService = new Mock<IDatabaseService>();

            _mockDbService.Setup(x => x.StoreTracebackAsync(It.IsAny<TracebackRecord>())).Returns(Task.CompletedTask);
            _mockDbService.Setup(x => x.StoreTracebackItemsAsync(It.IsAny<List<TracebackItem>>())).Returns(Task.CompletedTask);

            _ingestionService = new IngestionService(_mockTracebackService.Object, _mockDbService.Object, NullLogger<IngestionService>.Instance);
        }

        /// <summary>
        /// A successful run must finalize the record with the created/updated counts and one ledger entry per stored resource.
        /// </summary>
        [Test]
        public async Task IngestTracebackAsync_SuccessfulRun_FinalizesRecordWithCountsAndLedger()
        {
            // Arrange
            TracebackFetchResult fetchResult = new TracebackFetchResult();
            fetchResult.Document.Events = _testDocument.Events;
            fetchResult.Document.MasterData = _testDocument.MasterData;

            List<string> eventIds = _testDocument.Events.Select(e => e.EventID.ToString()).ToList();
            List<string> masterDataIds = _testDocument.MasterData.Select(m => m.ID).ToList();

            _mockTracebackService.Setup(x => x.TracebackAsync(It.IsAny<List<string>>(), It.IsAny<DigitalLinkQueryOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(fetchResult);
            _mockDbService.Setup(x => x.StoreEventsAsync(It.IsAny<List<IEvent>>())).ReturnsAsync((List<IEvent> batch) => new DatabaseStoreResult { CreatedIds = batch.Select(e => e.EventID.ToString()).ToList() });
            _mockDbService.Setup(x => x.StoreMasterDataAsync(It.IsAny<List<IVocabularyElement>>())).ReturnsAsync((List<IVocabularyElement> batch) => new DatabaseStoreResult { UpdatedIds = batch.Select(m => m.ID).ToList() });

            List<TracebackItem> storedItems = new List<TracebackItem>();
            _mockDbService.Setup(x => x.StoreTracebackItemsAsync(It.IsAny<List<TracebackItem>>())).Callback((List<TracebackItem> items) => storedItems.AddRange(items)).Returns(Task.CompletedTask);

            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" }, ResolverUrl = "https://resolver.example.com/" };

            // Act
            TracebackRecord record = await _ingestionService.IngestTracebackAsync(request, CancellationToken.None);

            // Assert
            Assert.That(record.Status, Is.EqualTo(TracebackStatus.Completed));
            Assert.That(record.EventsCreated, Is.EqualTo(eventIds.Count));
            Assert.That(record.EventsUpdated, Is.EqualTo(0));
            Assert.That(record.MasterDataCreated, Is.EqualTo(0));
            Assert.That(record.MasterDataUpdated, Is.EqualTo(masterDataIds.Count));
            Assert.That(record.EndTime, Is.Not.Null);

            Assert.That(storedItems.Count, Is.EqualTo(eventIds.Count + masterDataIds.Count), "One ledger entry per stored resource is expected.");
            Assert.That(storedItems.Where(i => i.ItemType == TracebackItemType.Event).All(i => i.Created), Is.True);
            Assert.That(storedItems.Where(i => i.ItemType == TracebackItemType.MasterData).All(i => !i.Created), Is.True);
            Assert.That(storedItems.All(i => i.TracebackId == record.Id), Is.True, "Every ledger entry must be keyed to the traceback record.");

            // The record is stored once when opened and once when finalized.
            _mockDbService.Verify(x => x.StoreTracebackAsync(It.IsAny<TracebackRecord>()), Times.Exactly(2));
        }

        /// <summary>
        /// Errors reported by the fetch must be persisted and the run marked CompletedWithErrors.
        /// </summary>
        [Test]
        public async Task IngestTracebackAsync_FetchReportsErrors_CompletedWithErrors()
        {
            // Arrange
            TracebackFetchResult fetchResult = new TracebackFetchResult();
            fetchResult.Errors.Add("Could not resolve the EPCIS query interface URL for urn:epc:id:sgtin:0614141.107346.2018.");

            _mockTracebackService.Setup(x => x.TracebackAsync(It.IsAny<List<string>>(), It.IsAny<DigitalLinkQueryOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(fetchResult);

            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" }, ResolverUrl = "https://resolver.example.com/" };

            // Act
            TracebackRecord record = await _ingestionService.IngestTracebackAsync(request, CancellationToken.None);

            // Assert
            Assert.That(record.Status, Is.EqualTo(TracebackStatus.CompletedWithErrors));
            Assert.That(record.Errors, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// A throwing fetch must not propagate; the run is recorded as Failed with an end time and the error message.
        /// </summary>
        [Test]
        public async Task IngestTracebackAsync_FetchThrows_RecordsFailedRun()
        {
            // Arrange
            _mockTracebackService.Setup(x => x.TracebackAsync(It.IsAny<List<string>>(), It.IsAny<DigitalLinkQueryOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("The external server is unreachable."));

            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" }, ResolverUrl = "https://resolver.example.com/" };

            // Act
            TracebackRecord record = await _ingestionService.IngestTracebackAsync(request, CancellationToken.None);

            // Assert
            Assert.That(record.Status, Is.EqualTo(TracebackStatus.Failed));
            Assert.That(record.EndTime, Is.Not.Null);
            Assert.That(record.Errors, Has.Some.Contains("unreachable"));
            _mockDbService.Verify(x => x.StoreTracebackAsync(It.IsAny<TracebackRecord>()), Times.Exactly(2));
        }

        /// <summary>
        /// A request without EPCs must be rejected before anything is stored.
        /// </summary>
        [Test]
        public void IngestTracebackAsync_NoEpcs_ThrowsArgumentException()
        {
            // Arrange
            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "   " } };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => _ingestionService.IngestTracebackAsync(request, CancellationToken.None));
            _mockDbService.Verify(x => x.StoreTracebackAsync(It.IsAny<TracebackRecord>()), Times.Never);
        }

        /// <summary>
        /// A request without a resolver URL must be rejected before anything is stored.
        /// </summary>
        [Test]
        public void IngestTracebackAsync_NoResolverUrl_ThrowsArgumentException()
        {
            // Arrange
            TracebackRequest request = new TracebackRequest { Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" } };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => _ingestionService.IngestTracebackAsync(request, CancellationToken.None));
            _mockDbService.Verify(x => x.StoreTracebackAsync(It.IsAny<TracebackRecord>()), Times.Never);
        }

        /// <summary>
        /// The request's resolver URL and the fixed 1.2.0 resolver version must flow into the query options.
        /// </summary>
        [Test]
        public async Task IngestTracebackAsync_RequestResolverUrl_PassedToQueryOptions()
        {
            // Arrange
            DigitalLinkQueryOptions? capturedOptions = null;
            _mockTracebackService.Setup(x => x.TracebackAsync(It.IsAny<List<string>>(), It.IsAny<DigitalLinkQueryOptions>(), It.IsAny<CancellationToken>()))
                .Callback((List<string> epcs, DigitalLinkQueryOptions options, CancellationToken ct) => capturedOptions = options)
                .ReturnsAsync(new TracebackFetchResult());

            TracebackRequest request = new TracebackRequest
            {
                Epcs = new List<string> { "urn:epc:id:sgtin:0614141.107346.2018" },
                ResolverUrl = "https://request-resolver.example.com/"
            };

            // Act
            TracebackRecord record = await _ingestionService.IngestTracebackAsync(request, CancellationToken.None);

            // Assert
            Assert.That(capturedOptions, Is.Not.Null);
            Assert.That(capturedOptions!.URL!.ToString(), Is.EqualTo("https://request-resolver.example.com/"));
            Assert.That(capturedOptions.ResolverVersion, Is.EqualTo(ResolverVersion.ResolverStandard_1_2_0), "The resolver version is fixed to the 1.2.0 standard.");
            Assert.That(record.ResolverUrl, Is.EqualTo("https://request-resolver.example.com/"));
        }
    }
}
