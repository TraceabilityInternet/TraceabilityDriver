using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenTraceability;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.MasterData;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="DigitalLinkService"/>.
    /// </summary>
    [TestFixture]
    public class DigitalLinkServiceTests
    {
        private const string BaseUrl = "https://driver.example.com";
        private const string Identifier = "urn:gdst:example.org:product:class:pollock";

        private Mock<IDatabaseService> _mockDbService = null!;
        private DigitalLinkService _service = null!;

        [SetUp]
        public void Setup()
        {
            _mockDbService = new Mock<IDatabaseService>();
            _service = CreateService(_mockDbService, BaseUrl);
        }

        /// <summary>
        /// Builds a service over the mocked data cache with the given configured base URL.
        /// </summary>
        private static DigitalLinkService CreateService(Mock<IDatabaseService> mockDbService, string baseUrl)
        {
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["URL"] = baseUrl }).Build();
            return new DigitalLinkService(mockDbService.Object, config, NullLogger<DigitalLinkService>.Instance);
        }

        /// <summary>
        /// Marks the identifier as existing in the data cache.
        /// </summary>
        private void SetupMasterDataFound()
        {
            _mockDbService.Setup(x => x.QueryMasterData(Identifier)).ReturnsAsync(new Mock<IVocabularyElement>().Object);
        }

        [Test]
        public async Task BuildLinksetAsync_ElementFound_ReturnsLinksetWithMasterDataAndEpcisLinks()
        {
            // Arrange
            SetupMasterDataFound();
            string anchor = $"{BaseUrl}/digitallink/01/{Identifier}";

            // Act
            Linkset? linkset = await _service.BuildLinksetAsync(Identifier, anchor);

            // Assert
            Assert.That(linkset, Is.Not.Null);
            Assert.That(linkset!.linkset, Has.Count.EqualTo(1));

            LinksetItem item = linkset.linkset[0];
            Assert.That(item.anchor, Is.EqualTo(anchor));
            Assert.That(item.GetLinks(DigitalLinkVocab.DefaultLinkUri)[0].href, Is.EqualTo($"{BaseUrl}/masterdata/{Identifier}"));
            Assert.That(item.GetLinks(DigitalLinkVocab.MasterDataUri)[0].href, Is.EqualTo($"{BaseUrl}/masterdata/{Identifier}"));
            Assert.That(item.GetLinks(DigitalLinkVocab.EpcisUri)[0].href, Is.EqualTo($"{BaseUrl}/epcis"));
        }

        [Test]
        public async Task BuildLinksetAsync_ElementNotFound_ReturnsNull()
        {
            // Arrange
            _mockDbService.Setup(x => x.QueryMasterData(Identifier)).ReturnsAsync((IVocabularyElement?)null);

            // Act
            Linkset? linkset = await _service.BuildLinksetAsync(Identifier, $"{BaseUrl}/digitallink/01/{Identifier}");

            // Assert
            Assert.That(linkset, Is.Null);
        }

        [Test]
        public async Task BuildLinksetAsync_NullIdentifier_ReturnsEpcisOnlyLinksetWithoutLookup()
        {
            // Act
            Linkset? linkset = await _service.BuildLinksetAsync(null, $"{BaseUrl}/digitallink");

            // Assert
            Assert.That(linkset, Is.Not.Null);

            LinksetItem item = linkset!.linkset[0];
            Assert.That(item.GetLinks(DigitalLinkVocab.DefaultLinkUri)[0].href, Is.EqualTo($"{BaseUrl}/epcis"));
            Assert.That(item.GetLinks(DigitalLinkVocab.EpcisUri)[0].href, Is.EqualTo($"{BaseUrl}/epcis"));
            Assert.That(item.linkTypes.ContainsKey(DigitalLinkVocab.MasterDataUri), Is.False, "An identifier without master data should not emit a master data link.");
            _mockDbService.Verify(x => x.QueryMasterData(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task BuildLinksetAsync_TrailingSlashBaseUrl_ProducesCleanHrefs()
        {
            // Arrange
            SetupMasterDataFound();
            DigitalLinkService service = CreateService(_mockDbService, BaseUrl + "/");

            // Act
            Linkset? linkset = await service.BuildLinksetAsync(Identifier, $"{BaseUrl}/digitallink/01/{Identifier}");

            // Assert
            Assert.That(linkset, Is.Not.Null);
            Assert.That(linkset!.linkset[0].GetLinks(DigitalLinkVocab.EpcisUri)[0].href, Is.EqualTo($"{BaseUrl}/epcis"));
            Assert.That(linkset.linkset[0].GetLinks(DigitalLinkVocab.MasterDataUri)[0].href, Is.EqualTo($"{BaseUrl}/masterdata/{Identifier}"));
        }

        /// <summary>
        /// Each supported link type (compact CURIE or full URI, any casing) resolves to its
        /// documented target; unsupported types resolve to null.
        /// </summary>
        [Test]
        [TestCase(null, "masterdata")]
        [TestCase("", "masterdata")]
        [TestCase("gs1:defaultLink", "masterdata")]
        [TestCase("gs1:masterData", "masterdata")]
        [TestCase("GS1:MASTERDATA", "masterdata")]
        [TestCase("https://ref.gs1.org/voc/masterData", "masterdata")]
        [TestCase("gs1:epcis", "epcis")]
        [TestCase("https://ref.gs1.org/voc/epcis", "epcis")]
        [TestCase("gs1:unknownLinkType", null)]
        public async Task ResolveTargetHrefAsync_ElementFound_ResolvesLinkTypeToTarget(string? linkType, string? expectedTarget)
        {
            // Arrange
            SetupMasterDataFound();
            string? expectedHref = expectedTarget switch
            {
                "masterdata" => $"{BaseUrl}/masterdata/{Identifier}",
                "epcis" => $"{BaseUrl}/epcis",
                _ => null
            };

            // Act
            string? target = await _service.ResolveTargetHrefAsync(Identifier, linkType);

            // Assert
            Assert.That(target, Is.EqualTo(expectedHref));
        }

        [Test]
        public async Task ResolveTargetHrefAsync_NullIdentifier_DefaultsToEpcis()
        {
            // Act
            string? target = await _service.ResolveTargetHrefAsync(null, null);

            // Assert
            Assert.That(target, Is.EqualTo($"{BaseUrl}/epcis"));
            _mockDbService.Verify(x => x.QueryMasterData(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ResolveTargetHrefAsync_NullIdentifierWithMasterDataLinkType_ReturnsNull()
        {
            // Act
            string? target = await _service.ResolveTargetHrefAsync(null, "gs1:masterData");

            // Assert
            Assert.That(target, Is.Null, "An identifier without master data has no master data target to redirect to.");
        }

        [Test]
        public async Task ResolveTargetHrefAsync_ElementNotFound_ReturnsNull()
        {
            // Arrange
            _mockDbService.Setup(x => x.QueryMasterData(Identifier)).ReturnsAsync((IVocabularyElement?)null);

            // Act
            string? target = await _service.ResolveTargetHrefAsync(Identifier, null);

            // Assert
            Assert.That(target, Is.Null);
        }

        [Test]
        public void Constructor_WithNullDatabaseService_ThrowsArgumentNullException()
        {
            // Arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // Act & Assert
            ArgumentNullException? exception = Assert.Throws<ArgumentNullException>(() => new DigitalLinkService(null!, config, NullLogger<DigitalLinkService>.Instance));

            Assert.That(exception!.ParamName, Is.EqualTo("dbService"));
        }
    }
}
