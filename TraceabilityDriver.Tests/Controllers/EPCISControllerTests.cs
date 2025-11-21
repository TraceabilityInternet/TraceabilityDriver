using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Controllers;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Controllers
{
    [TestFixture]
    public class EPCISControllerTests
    {
        private Mock<IDatabaseService> _mockDbService;
        private Mock<ILogger<EPCISController>> _mockLogger;
        private EPCISController _controller;

        [SetUp]
        public void Setup()
        {
            _mockDbService = new Mock<IDatabaseService>();
            _mockLogger = new Mock<ILogger<EPCISController>>();
            
            _mockDbService.Setup(x => x.InitializeDatabase()).Returns(Task.CompletedTask);
            
            _controller = new EPCISController(_mockDbService.Object, _mockLogger.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Test]
        public async Task SimpleQueryPost_WithValidParameters_ReturnsOk()
        {
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery()
            };
            
            var queryDoc = new EPCISQueryDocument
            {
                EPCISVersion = EPCISVersion.V2,
                Events = new List<OpenTraceability.Interfaces.IEvent>()
            };

            _mockDbService.Setup(x => x.QueryEvents(It.IsAny<EPCISQueryParameters>()))
                .ReturnsAsync(queryDoc);

            _controller.HttpContext.Request.Headers["GS1-EPCIS-Version"] = "2.0";
            _controller.HttpContext.Response.Body = new MemoryStream();

            var result = await _controller.SimpleQueryPost(queryParams);

            Assert.That(result, Is.InstanceOf<EmptyResult>());
            _mockDbService.Verify(x => x.QueryEvents(queryParams), Times.Once);
        }

        [Test]
        public async Task SimpleQueryPost_WithException_ReturnsProblem()
        {
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery()
            };

            _mockDbService.Setup(x => x.QueryEvents(It.IsAny<EPCISQueryParameters>()))
                .ThrowsAsync(new Exception("Database error"));

            var result = await _controller.SimpleQueryPost(queryParams);

            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task SimpleQueryGet_WithValidUrl_ReturnsOk()
        {
            var queryDoc = new EPCISQueryDocument
            {
                EPCISVersion = EPCISVersion.V2,
                Events = new List<OpenTraceability.Interfaces.IEvent>()
            };

            _mockDbService.Setup(x => x.QueryEvents(It.IsAny<EPCISQueryParameters>()))
                .ReturnsAsync(queryDoc);

            _controller.HttpContext.Request.Scheme = "https";
            _controller.HttpContext.Request.Host = new HostString("localhost");
            _controller.HttpContext.Request.Path = "/epcis/events";
            _controller.HttpContext.Request.QueryString = new QueryString("");
            _controller.HttpContext.Request.Headers["GS1-EPCIS-Version"] = "2.0";
            _controller.HttpContext.Response.Body = new MemoryStream();

            var result = await _controller.SimpleQueryGet();

            Assert.That(result, Is.InstanceOf<EmptyResult>());
            _mockDbService.Verify(x => x.QueryEvents(It.IsAny<EPCISQueryParameters>()), Times.Once);
        }

        [Test]
        public async Task SimpleQueryGet_WithException_ReturnsProblem()
        {
            _mockDbService.Setup(x => x.QueryEvents(It.IsAny<EPCISQueryParameters>()))
                .ThrowsAsync(new Exception("Query error"));

            _controller.HttpContext.Request.Scheme = "https";
            _controller.HttpContext.Request.Host = new HostString("localhost");
            _controller.HttpContext.Request.Path = "/epcis/events";

            var result = await _controller.SimpleQueryGet();

            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task SimpleQueryPost_WithGS1Version12_UsesXmlMapper()
        {
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery()
            };

            var queryDoc = new EPCISQueryDocument
            {
                EPCISVersion = EPCISVersion.V1,
                CreationDate = DateTime.UtcNow,
                Events = new List<OpenTraceability.Interfaces.IEvent>()
            };

            _mockDbService.Setup(x => x.QueryEvents(It.IsAny<EPCISQueryParameters>()))
                .ReturnsAsync(queryDoc);

            _controller.HttpContext.Request.Headers["GS1-EPCIS-Version"] = "1.2";
            _controller.HttpContext.Response.Body = new MemoryStream();

            var result = await _controller.SimpleQueryPost(queryParams);

            Assert.That(result, Is.InstanceOf<EmptyResult>());
            Assert.That(_controller.HttpContext.Response.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task SimpleQueryPost_WithGS1HeadersInRequest_CopiesHeadersToResponse()
        {
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery()
            };

            var queryDoc = new EPCISQueryDocument
            {
                EPCISVersion = EPCISVersion.V2,
                Events = new List<OpenTraceability.Interfaces.IEvent>()
            };

            _mockDbService.Setup(x => x.QueryEvents(It.IsAny<EPCISQueryParameters>()))
                .ReturnsAsync(queryDoc);

            _controller.HttpContext.Request.Headers["GS1-EPCIS-Version"] = "2.0";
            _controller.HttpContext.Request.Headers["GS1-Extensions"] = "test";
            _controller.HttpContext.Response.Body = new MemoryStream();

            var result = await _controller.SimpleQueryPost(queryParams);

            Assert.That(result, Is.InstanceOf<EmptyResult>());
            Assert.That(_controller.HttpContext.Response.Headers.ContainsKey("GS1-EPCIS-Version"), Is.True);
            Assert.That(_controller.HttpContext.Response.Headers.ContainsKey("GS1-Extensions"), Is.True);
        }

        [Test]
        public async Task SimpleQueryPost_SetsCorrectEPCISVersion()
        {
            var queryParams = new EPCISQueryParameters
            {
                query = new EPCISQuery()
            };

            var queryDoc = new EPCISQueryDocument
            {
                EPCISVersion = EPCISVersion.V1,
                Events = new List<OpenTraceability.Interfaces.IEvent>()
            };

            _mockDbService.Setup(x => x.QueryEvents(It.IsAny<EPCISQueryParameters>()))
                .ReturnsAsync(queryDoc);

            _controller.HttpContext.Request.Headers["GS1-EPCIS-Version"] = "2.0";
            _controller.HttpContext.Response.Body = new MemoryStream();

            await _controller.SimpleQueryPost(queryParams);

            Assert.That(queryDoc.EPCISVersion, Is.EqualTo(EPCISVersion.V2));
        }

        [Test]
        public async Task SimpleQueryGet_CallsDatabaseService()
        {
            var queryDoc = new EPCISQueryDocument
            {
                EPCISVersion = EPCISVersion.V2,
                Events = new List<OpenTraceability.Interfaces.IEvent>()
            };

            _mockDbService.Setup(x => x.QueryEvents(It.IsAny<EPCISQueryParameters>()))
                .ReturnsAsync(queryDoc);

            _controller.HttpContext.Request.Scheme = "https";
            _controller.HttpContext.Request.Host = new HostString("localhost");
            _controller.HttpContext.Request.Path = "/epcis/events";
            _controller.HttpContext.Request.QueryString = new QueryString("?eventType=ObjectEvent");
            _controller.HttpContext.Request.Headers["GS1-EPCIS-Version"] = "2.0";
            _controller.HttpContext.Response.Body = new MemoryStream();

            await _controller.SimpleQueryGet();

            _mockDbService.Verify(x => x.QueryEvents(It.IsAny<EPCISQueryParameters>()), Times.Once);
        }
    }
}
