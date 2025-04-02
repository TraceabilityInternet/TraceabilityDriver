using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using TraceabilityDriver.Services.Mapping.Functions;
using Microsoft.Extensions.Logging;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services.Mapping.Functions
{
    [TestFixture]
    public class DictionaryMappingFunctionTests
    {
        private Mock<ISynchronizationContext> _mockContext;
        private Mock<ILogger<DictionaryMappingFunction>> _mockLogger;
        private DictionaryMappingFunction _function;
        private TDMappingConfiguration _mockConfiguration;

        [SetUp]
        public void Setup()
        {
            _mockContext = new Mock<ISynchronizationContext>();
            _mockLogger = new Mock<ILogger<DictionaryMappingFunction>>();
            _function = new DictionaryMappingFunction(_mockContext.Object, _mockLogger.Object);
            
            // Setup mock configuration with test dictionaries
            _mockConfiguration = new TDMappingConfiguration
            {
                Dictionaries = new Dictionary<string, Dictionary<string, string>>
                {
                    ["testDict"] = new Dictionary<string, string>
                    {
                        ["key1"] = "value1",
                        ["key2"] = "value2"
                    }
                }
            };
            
            _mockContext.Setup(c => c.Configuration).Returns(_mockConfiguration);
        }

        [Test]
        public void Execute_WithNullParameters_ThrowsArgumentNullException()
        {
            // Arrange
            List<string?> parameters = null!;

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => _function.Execute(parameters!));
        }

        [Test]
        public void Execute_WithInsufficientParameters_ReturnsNull()
        {
            // Arrange
            var parameters = new List<string?> { "value" };

            // Act
            var result = _function.Execute(parameters);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Execute_WithEmptyDictionaryName_ReturnsNull()
        {
            // Arrange
            var parameters = new List<string?> { "key1", "" };

            // Act
            var result = _function.Execute(parameters);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Execute_WithWhitespaceDictionaryName_ReturnsNull()
        {
            // Arrange
            var parameters = new List<string?> { "key1", "   " };

            // Act
            var result = _function.Execute(parameters);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Execute_WithEmptyValue_ReturnsNull()
        {
            // Arrange
            var parameters = new List<string?> { "", "testDict" };

            // Act
            var result = _function.Execute(parameters);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Execute_WithNullValue_ReturnsNull()
        {
            // Arrange
            var parameters = new List<string?> { null, "testDict" };

            // Act
            var result = _function.Execute(parameters);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Execute_WithNonExistentDictionary_ReturnsNull()
        {
            // Arrange
            var parameters = new List<string?> { "key1", "nonExistentDict" };

            // Act
            var result = _function.Execute(parameters);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Execute_WithNonExistentKey_ReturnsNull()
        {
            // Arrange
            var parameters = new List<string?> { "nonExistentKey", "testDict" };

            // Act
            var result = _function.Execute(parameters);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Execute_WithValidParameters_ReturnsExpectedValue()
        {
            // Arrange
            var parameters = new List<string?> { "key1", "testDict" };

            // Act
            var result = _function.Execute(parameters);

            // Assert
            Assert.That(result, Is.EqualTo("value1"));
        }

        [Test]
        public void Execute_WithNullConfiguration_ReturnsNull()
        {
            // Arrange
            _mockContext.Setup(c => c.Configuration).Returns(() => null);
            var parameters = new List<string?> { "key1", "testDict" };

            // Act
            var result = _function.Execute(parameters);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Initialize_DoesNothing()
        {
            // Act & Assert (no exception should be thrown)
            Assert.DoesNotThrow(() => _function.Initialize());
        }
    }
}
