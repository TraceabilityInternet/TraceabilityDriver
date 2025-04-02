using NUnit.Framework;
using System.Collections.Generic;
using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver.Tests.Services.Mapping.Functions
{
    [TestFixture]
    public class GenerateIdentifierFunctionTests
    {
        private GenerateIdentifierFunction? _function;

        [SetUp]
        public void Setup()
        {
            _function = new GenerateIdentifierFunction();
            _function.Initialize();
        }

        [Test]
        public void Execute_WithValidInputs_JoinsWithDashes()
        {
            // Arrange
            var parameters = new List<string?> { "abc", "123", "def" };

            // Act
            var result = _function!.Execute(parameters);

            // Assert
            Assert.That(result, Is.EqualTo("abc-123-def"));
        }

        [Test]
        public void Execute_WithSpecialCharacters_StripsNonAlphanumeric()
        {
            // Arrange
            var parameters = new List<string?> { "a#b!c", "1@2$3", "d%e^f" };

            // Act
            var result = _function!.Execute(parameters);

            // Assert
            Assert.That(result, Is.EqualTo("abc-123-def"));
        }

        [Test]
        public void Execute_WithNullValues_TreatsAsEmpty()
        {
            // Arrange
            var parameters = new List<string?> { "abc", null, "def" };

            // Act
            var result = _function!.Execute(parameters);

            // Assert
            Assert.That(result, Is.EqualTo("abc--def"));
        }

        [Test]
        public void Execute_WithEmptyStrings_ReturnsEmptySegments()
        {
            // Arrange
            var parameters = new List<string?> { "abc", "", "def" };

            // Act
            var result = _function!.Execute(parameters);

            // Assert
            Assert.That(result, Is.EqualTo("abc--def"));
        }

        [Test]
        public void Execute_WithMixedInputs_HandlesCorrectly()
        {
            // Arrange
            var parameters = new List<string?> { "a b c", "1-2-3", "d.e.f" };

            // Act
            var result = _function!.Execute(parameters);

            // Assert
            Assert.That(result, Is.EqualTo("abc-123-def"));
        }

        [Test]
        public void Execute_WithEmptyList_ReturnsEmptyString()
        {
            // Arrange
            var parameters = new List<string?>();

            // Act
            var result = _function!.Execute(parameters);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }
    }
}
