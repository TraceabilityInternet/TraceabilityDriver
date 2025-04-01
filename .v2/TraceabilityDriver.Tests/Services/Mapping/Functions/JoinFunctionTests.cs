using NUnit.Framework;
using System.Collections.Generic;
using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver.Tests.Services.Mapping.Functions
{
    [TestFixture]
    public class JoinFunctionTests
    {
        private JoinFunction _joinFunction;

        [SetUp]
        public void Setup()
        {
            _joinFunction = new JoinFunction();
        }

        [Test]
        public void Execute_WithMultipleParameters_ReturnsJoinedString()
        {
            // Arrange
            var parameters = new List<string?> { ",", "apple", "banana", "cherry" };

            // Act
            var result = _joinFunction.Execute(parameters);

            // Assert
            Assert.That(result, Is.EqualTo("apple,banana,cherry"));
        }

        [Test]
        public void Execute_WithSingleParameter_ReturnsNull()
        {
            // Arrange
            var parameters = new List<string?> { "," };

            // Act
            var result = _joinFunction.Execute(parameters);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Execute_WithEmptyList_ReturnsNull()
        {
            // Arrange
            var parameters = new List<string?>();

            // Act
            var result = _joinFunction.Execute(parameters);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Execute_WithNullValues_JoinsWithEmptyStrings()
        {
            // Arrange
            var parameters = new List<string?> { "-", "apple", null, "cherry" };

            // Act
            var result = _joinFunction.Execute(parameters);

            // Assert
            Assert.That(result, Is.EqualTo("apple-cherry"));
        }

        [Test]
        public void Initialize_DoesNothing()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _joinFunction.Initialize());
        }
    }
}
