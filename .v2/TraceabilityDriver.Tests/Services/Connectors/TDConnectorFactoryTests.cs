using Castle.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Connectors;

namespace TraceabilityDriver.Tests.Services.Connectors
{
    [TestFixture]
    public class TDConnectorFactoryTests
    {
        private readonly Mock<ILogger<TDConnectorFactory>> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly TDConnectorFactory _factory;
        private readonly TDSqlServerConnector _mockConnector;

        public TDConnectorFactoryTests()
        {
            _mockLogger = new Mock<ILogger<TDConnectorFactory>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _factory = new TDConnectorFactory(_mockLogger.Object, _mockServiceProvider.Object);

            // Create a mock TDSqlServerConnector.
            Mock<IOptions<TDConnectorConfiguration>> mockOptions = new Mock<IOptions<TDConnectorConfiguration>>();
            Mock<ILogger<TDSqlServerConnector>> mockLogger = new Mock<ILogger<TDSqlServerConnector>>();
            Mock<IEventsTableMappingService> mockEventsTableMappingService = new Mock<IEventsTableMappingService>();
            _mockConnector = new TDSqlServerConnector(mockLogger.Object, mockEventsTableMappingService.Object);
        }

        [Test]
        public void CreateConnector_SqlServerConnectorType_ReturnsConnector()
        {
            // Arrange
            var config = new TDConnectorConfiguration
            {
                ConnectorType = ConnectorType.SqlServer,
                Database = "TestDb"
            };

            _mockServiceProvider.Reset();
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(TDSqlServerConnector)))
                .Returns(_mockConnector);

            // Act
            var result = _factory.CreateConnector(config);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<TDSqlServerConnector>());
            _mockServiceProvider.Verify(sp => sp.GetService(typeof(TDSqlServerConnector)), Times.Once);
        }

        [Test]
        public void CreateConnector_UnsupportedConnectorType_ThrowsException()
        {
            // Arrange
            var config = new TDConnectorConfiguration
            {
                ConnectorType = (ConnectorType)999, // Invalid enum value
                Database = "TestDb"
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _factory.CreateConnector(config));
            Assert.That(exception.InnerException?.Message, Does.Contain($"Unsupported connector type: {config.ConnectorType}"));
        }

        [Test]
        public void CreateConnector_ServiceProviderThrowsException_WrapsExceptionWithDetails()
        {
            // Arrange
            var config = new TDConnectorConfiguration
            {
                ConnectorType = ConnectorType.SqlServer,
                Database = "TestDb"
            };

            var serviceException = new InvalidOperationException("Service not registered");
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(TDSqlServerConnector)))
                .Throws(serviceException);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _factory.CreateConnector(config));
            Assert.That(exception.Message, Does.Contain($"Error creating a connector for the connector configuration: {config.Database}"));
            Assert.That(exception.InnerException, Is.SameAs(serviceException));
        }
    }
}
