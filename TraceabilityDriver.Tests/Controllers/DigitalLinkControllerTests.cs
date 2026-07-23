using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTraceability;
using OpenTraceability.Models.MasterData;
using TraceabilityDriver.Controllers;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Controllers
{
    /// <summary>
    /// Unit tests for <see cref="DigitalLinkController"/>.
    /// </summary>
    [TestFixture]
    public class DigitalLinkControllerTests
    {
        private const string Gtin = "urn:gdst:example.org:product:class:pollock";
        private const string TargetHref = "https://driver.example.com/masterdata/" + Gtin;

        private Mock<IDigitalLinkService> _mockDigitalLink = null!;
        private DigitalLinkController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _mockDigitalLink = new Mock<IDigitalLinkService>();
            _controller = new DigitalLinkController(_mockDigitalLink.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            _controller.HttpContext.Request.Scheme = "https";
            _controller.HttpContext.Request.Host = new HostString("localhost");
            _controller.HttpContext.Request.Path = $"/digitallink/01/{Gtin}";
        }

        /// <summary>
        /// Builds a minimal linkset carrying a single EPCIS link, as the service would return it.
        /// </summary>
        private static Linkset CreateLinkset(string anchor)
        {
            LinksetItem item = new LinksetItem { anchor = anchor };
            item.linkTypes[DigitalLinkVocab.EpcisUri] = new JArray { new JObject { ["href"] = "https://driver.example.com/epcis", ["title"] = "EPCIS Repository" } };
            return new Linkset { linkset = new List<LinksetItem> { item } };
        }

        /// <summary>
        /// Asserts the result is a linkset content response and round-trips it back through
        /// Newtonsoft to prove the dynamic link type keys survived serialization.
        /// </summary>
        private static Linkset AssertLinksetContent(IActionResult result)
        {
            Assert.That(result, Is.InstanceOf<ContentResult>());
            ContentResult contentResult = (ContentResult)result;
            Assert.That(contentResult.ContentType, Is.EqualTo(DigitalLinkVocab.LinksetMediaType));

            Linkset? roundTripped = JsonConvert.DeserializeObject<Linkset>(contentResult.Content!);
            Assert.That(roundTripped, Is.Not.Null);
            Assert.That(roundTripped!.linkset[0].GetLinks(DigitalLinkVocab.EpcisUri), Is.Not.Empty, "The dynamic link type keys must survive serialization.");
            return roundTripped;
        }

        [Test]
        public async Task GTIN_AcceptLinksetJson_ReturnsLinksetContent()
        {
            // Arrange
            string expectedAnchor = $"https://localhost/digitallink/01/{Gtin}";
            _controller.HttpContext.Request.Headers["Accept"] = DigitalLinkVocab.LinksetMediaType;
            _mockDigitalLink.Setup(x => x.BuildLinksetAsync(Gtin, expectedAnchor)).ReturnsAsync(CreateLinkset(expectedAnchor));

            // Act
            IActionResult result = await _controller.GTIN(Gtin, null);

            // Assert
            Linkset linkset = AssertLinksetContent(result);
            Assert.That(linkset.linkset[0].anchor, Is.EqualTo(expectedAnchor));
            _mockDigitalLink.Verify(x => x.BuildLinksetAsync(Gtin, expectedAnchor), Times.Once);
        }

        [Test]
        public async Task GTIN_AcceptApplicationJson_ReturnsLinksetContent()
        {
            // Arrange
            _controller.HttpContext.Request.Headers["Accept"] = "application/json";
            _mockDigitalLink.Setup(x => x.BuildLinksetAsync(Gtin, It.IsAny<string>())).ReturnsAsync(CreateLinkset("anchor"));

            // Act
            IActionResult result = await _controller.GTIN(Gtin, null);

            // Assert
            AssertLinksetContent(result);
        }

        [Test]
        public async Task GTIN_LinkTypeLinkset_ReturnsLinksetContent()
        {
            // Arrange
            _mockDigitalLink.Setup(x => x.BuildLinksetAsync(Gtin, It.IsAny<string>())).ReturnsAsync(CreateLinkset("anchor"));

            // Act
            IActionResult result = await _controller.GTIN(Gtin, "linkset");

            // Assert
            AssertLinksetContent(result);
            _mockDigitalLink.Verify(x => x.ResolveTargetHrefAsync(It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        }

        [Test]
        public async Task GTIN_NoLinksetRequested_RedirectsToTargetWithQueryString()
        {
            // Arrange
            _controller.HttpContext.Request.QueryString = new QueryString("?foo=bar");
            _mockDigitalLink.Setup(x => x.ResolveTargetHrefAsync(Gtin, null)).ReturnsAsync(TargetHref);

            // Act
            IActionResult result = await _controller.GTIN(Gtin, null);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectResult>());
            Assert.That(((RedirectResult)result).Url, Is.EqualTo(TargetHref + "?foo=bar"));
        }

        [Test]
        public async Task GTIN_UnknownLinkType_ReturnsNotFound()
        {
            // Arrange
            _mockDigitalLink.Setup(x => x.ResolveTargetHrefAsync(Gtin, "gs1:unknownLinkType")).ReturnsAsync((string?)null);

            // Act
            IActionResult result = await _controller.GTIN(Gtin, "gs1:unknownLinkType");

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task GTIN_ElementNotFound_LinksetMode_ReturnsNotFound()
        {
            // Arrange
            _controller.HttpContext.Request.Headers["Accept"] = DigitalLinkVocab.LinksetMediaType;
            _mockDigitalLink.Setup(x => x.BuildLinksetAsync(Gtin, It.IsAny<string>())).ReturnsAsync((Linkset?)null);

            // Act
            IActionResult result = await _controller.GTIN(Gtin, null);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task GTIN_ElementNotFound_RedirectMode_ReturnsNotFound()
        {
            // Arrange
            _mockDigitalLink.Setup(x => x.ResolveTargetHrefAsync(Gtin, null)).ReturnsAsync((string?)null);

            // Act
            IActionResult result = await _controller.GTIN(Gtin, null);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task GLN_ElementNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockDigitalLink.Setup(x => x.ResolveTargetHrefAsync("gln-1", null)).ReturnsAsync((string?)null);

            // Act
            IActionResult result = await _controller.GLN("gln-1", null);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
            _mockDigitalLink.Verify(x => x.ResolveTargetHrefAsync("gln-1", null), Times.Once);
        }

        [Test]
        public async Task PGLN_ElementNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockDigitalLink.Setup(x => x.ResolveTargetHrefAsync("pgln-1", null)).ReturnsAsync((string?)null);

            // Act
            IActionResult result = await _controller.PGLN("pgln-1", null);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
            _mockDigitalLink.Verify(x => x.ResolveTargetHrefAsync("pgln-1", null), Times.Once);
        }

        [Test]
        public async Task InstanceEPC_LooksUpTradeItemByGtin()
        {
            // Arrange
            _controller.HttpContext.Request.Headers["Accept"] = DigitalLinkVocab.LinksetMediaType;
            _mockDigitalLink.Setup(x => x.BuildLinksetAsync(Gtin, It.IsAny<string>())).ReturnsAsync(CreateLinkset("anchor"));

            // Act
            IActionResult result = await _controller.InstanceEPC(Gtin, "serial-1", null);

            // Assert
            AssertLinksetContent(result);
            _mockDigitalLink.Verify(x => x.BuildLinksetAsync(Gtin, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task ClassEPC_ElementNotFound_ReturnsNotFound()
        {
            // Arrange
            _controller.HttpContext.Request.Headers["Accept"] = DigitalLinkVocab.LinksetMediaType;
            _mockDigitalLink.Setup(x => x.BuildLinksetAsync(Gtin, It.IsAny<string>())).ReturnsAsync((Linkset?)null);

            // Act
            IActionResult result = await _controller.ClassEPC(Gtin, "lot-1", null);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
            _mockDigitalLink.Verify(x => x.BuildLinksetAsync(Gtin, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task SSCC_AcceptLinksetJson_PassesNullIdentifier()
        {
            // Arrange
            _controller.HttpContext.Request.Headers["Accept"] = DigitalLinkVocab.LinksetMediaType;
            _mockDigitalLink.Setup(x => x.BuildLinksetAsync(null, It.IsAny<string>())).ReturnsAsync(CreateLinkset("anchor"));

            // Act
            IActionResult result = await _controller.SSCC("sscc-1", null);

            // Assert
            AssertLinksetContent(result);
            _mockDigitalLink.Verify(x => x.BuildLinksetAsync(null, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task Get_Root_AcceptLinksetJson_PassesNullIdentifier()
        {
            // Arrange
            _controller.HttpContext.Request.Headers["Accept"] = DigitalLinkVocab.LinksetMediaType;
            _mockDigitalLink.Setup(x => x.BuildLinksetAsync(null, It.IsAny<string>())).ReturnsAsync(CreateLinkset("anchor"));

            // Act
            IActionResult result = await _controller.Get(null);

            // Assert
            AssertLinksetContent(result);
            _mockDigitalLink.Verify(x => x.BuildLinksetAsync(null, It.IsAny<string>()), Times.Once);
        }
    }
}
