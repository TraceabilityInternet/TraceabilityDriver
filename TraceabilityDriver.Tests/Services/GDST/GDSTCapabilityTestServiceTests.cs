using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using OpenTraceability.Interfaces;
using System.Net;
using TraceabilityDriver.Models.GDST;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.GDST;

namespace TraceabilityDriver.Tests.Services.GDST
{
    /// <summary>
    /// Unit tests for <see cref="GDSTCapabilityTestService"/>.
    /// </summary>
    /// <remarks>
    /// These tests cover the two contracts that matter now that the service no longer seeds example data:
    /// the supplied solution provider EPCs must be validated before any call to the capability tool, and
    /// they must be forwarded verbatim in the start request. The capability tool is stubbed at the
    /// <see cref="HttpMessageHandler"/> level, which rejects the start request — the run then returns a
    /// failed result, and the assertions are made against the captured request instead.
    /// </remarks>
    [TestFixture]
    [Category("UnitTest")]
    public class GDSTCapabilityTestServiceTests
    {
        private const string ValidEPC = "urn:epc:id:sscc:0860003130.0000000001";

        private Mock<IDatabaseService> _mockDatabase = null!;
        private Mock<IHttpClientFactory> _mockHttpClientFactory = null!;
        private Mock<ITracebackService> _mockTracebackService = null!;
        private string? _capturedStartRequestBody;
        private GDSTCapabilityTestService _service = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            OpenTraceability.GDST.Setup.Initialize();
        }

        /// <summary>
        /// Builds a service whose capability tool rejects the start request, capturing the request body.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _capturedStartRequestBody = null;

            Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage request, CancellationToken cancellationToken) =>
                {
                    if (request.Content != null)
                    {
                        _capturedStartRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
                    }

                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Rejected by the stub.") };
                });

            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            // The parameterless CreateClient() is an extension over CreateClient(Options.DefaultName), so
            // this single setup covers the call the service actually makes.
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(mockHandler.Object));

            _mockDatabase = new Mock<IDatabaseService>();
            _mockTracebackService = new Mock<ITracebackService>();

            IOptions<GDSTCapabilityTestSettings> settings = Options.Create(new GDSTCapabilityTestSettings
            {
                Url = "https://capabilitytool.example.com/",
                ApiKey = "test-api-key",
                SolutionName = "TraceabilityDriver",
                PGLN = "urn:gdst:example.org:party:TraceabilityDriver.001"
            });

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["URL"] = "http://localhost:5000" })
                .Build();

            _service = new GDSTCapabilityTestService(
                NullLogger<GDSTCapabilityTestService>.Instance,
                _mockDatabase.Object,
                _mockHttpClientFactory.Object,
                _mockTracebackService.Object,
                settings,
                configuration);
        }

        /// <summary>
        /// A null EPC list is an argument problem, not a test failure, so it must throw.
        /// </summary>
        [Test]
        public void RunTestAsync_WithNullList_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(() => _service.RunTestAsync(null!));

            Assert.That(exception!.ParamName, Is.EqualTo("solutionProviderEPCs"));
        }

        /// <summary>
        /// An empty list carries no EPC for the tool to trace, so it must throw.
        /// </summary>
        [Test]
        public void RunTestAsync_WithEmptyList_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(() => _service.RunTestAsync(new List<string>()));

            Assert.That(exception!.ParamName, Is.EqualTo("solutionProviderEPCs"));
        }

        /// <summary>
        /// A list holding only blank entries is treated the same as an empty list.
        /// </summary>
        [Test]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t")]
        public void RunTestAsync_WithBlankEntriesOnly_ThrowsArgumentException(string epc)
        {
            // Act & Assert
            ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(() => _service.RunTestAsync(new List<string> { epc }));

            Assert.That(exception!.ParamName, Is.EqualTo("solutionProviderEPCs"));
            Assert.That(exception.Message, Does.Contain("At least one solution provider EPC is required"));
        }

        /// <summary>
        /// A malformed EPC must be rejected with the reason from the EPC parser, so the user can fix it.
        /// </summary>
        /// <remarks>
        /// The cases cover the two ways the OpenTraceability parser rejects a string: text that is not a
        /// URI at all, and a recognized EPC scheme missing a required segment (here, an LGTIN with no lot
        /// number). Note that the parser accepts any well-formed absolute URI, so an unknown-but-valid URN
        /// such as urn:epc:id:bogus:1.2 passes validation and is left for the capability tool to reject.
        /// </remarks>
        [Test]
        [TestCase("not-an-epc")]
        [TestCase("12345")]
        [TestCase("urn:epc:class:lgtin:0614141.107346")]
        public void RunTestAsync_WithMalformedEPC_ThrowsArgumentException(string epc)
        {
            // Act & Assert
            ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(() => _service.RunTestAsync(new List<string> { epc }));

            Assert.That(exception!.ParamName, Is.EqualTo("solutionProviderEPCs"));
            Assert.That(exception.Message, Does.Contain(epc), "The message should name the offending EPC.");
        }

        /// <summary>
        /// One bad EPC in an otherwise valid list must fail the whole request rather than be dropped.
        /// </summary>
        [Test]
        public void RunTestAsync_WithOneMalformedEPCAmongValidOnes_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(() => _service.RunTestAsync(new List<string> { ValidEPC, "not-an-epc" }));

            Assert.That(exception!.Message, Does.Contain("not-an-epc"));
        }

        /// <summary>
        /// Validation runs before any I/O, so an invalid EPC must never reach the capability tool.
        /// </summary>
        [Test]
        public void RunTestAsync_WithInvalidEPC_DoesNotCallTheCapabilityTool()
        {
            // Act
            Assert.ThrowsAsync<ArgumentException>(() => _service.RunTestAsync(new List<string> { "not-an-epc" }));

            // Assert
            _mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// The EPCs the caller supplied must be the EPCs the tool is asked to trace back, with their
        /// original casing intact. This is the regression guard against the previously hardcoded EPC.
        /// </summary>
        [Test]
        public async Task RunTestAsync_WithValidEPCs_SendsTheSuppliedEPCsAsTheSolutionProviderEPCs()
        {
            // Arrange
            List<string> epcs = new List<string> { ValidEPC, "urn:epc:id:sscc:0860003130.SerialCase01" };

            // Act
            GDSTCapabilityTestResults results = await _service.RunTestAsync(epcs);

            // Assert
            Assert.That(_capturedStartRequestBody, Is.Not.Null, "The service should have posted a start request.");

            GDSTCapabilityTestModel? startModel = JsonConvert.DeserializeObject<GDSTCapabilityTestModel>(_capturedStartRequestBody!);

            Assert.That(startModel, Is.Not.Null);
            Assert.That(startModel!.SolutionProviderEPCs, Is.EqualTo(epcs), "The supplied EPCs must be forwarded verbatim, preserving casing.");
            Assert.That(results.Status, Is.EqualTo(GDSTCapabilityTestStatus.Failed), "The stubbed tool rejects the start request, so the run fails.");
        }

        /// <summary>
        /// Surrounding whitespace from a pasted EPC must be trimmed before the request is sent.
        /// </summary>
        [Test]
        public async Task RunTestAsync_WithPaddedEPC_TrimsBeforeSending()
        {
            // Act
            await _service.RunTestAsync(new List<string> { $"  {ValidEPC}  ", "   " });

            // Assert
            GDSTCapabilityTestModel? startModel = JsonConvert.DeserializeObject<GDSTCapabilityTestModel>(_capturedStartRequestBody!);

            Assert.That(startModel!.SolutionProviderEPCs, Is.EqualTo(new List<string> { ValidEPC }), "Blank entries should be dropped and the remaining EPC trimmed.");
        }

        /// <summary>
        /// The service must no longer seed example data — the whole point is to test the synced data.
        /// </summary>
        [Test]
        public async Task RunTestAsync_DoesNotSeedTestDataIntoTheDatabase()
        {
            // Act
            await _service.RunTestAsync(new List<string> { ValidEPC });

            // Assert
            _mockDatabase.Verify(d => d.StoreTracebackEventsAsync(It.IsAny<List<IEvent>>()), Times.Never);
            _mockDatabase.Verify(d => d.StoreTracebackMasterDataAsync(It.IsAny<List<IVocabularyElement>>()), Times.Never);
        }
    }
}
