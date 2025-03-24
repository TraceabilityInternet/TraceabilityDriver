using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Tests.Models.Mapping
{
    [TestFixture]
    public class TDEventMappingTests
    {
        [Test]
        public void GenerateFields_WithValidJson_ShouldReturnTrue()
        {
            // Arrange
            var json = new JObject
            {
                ["EventId"] = "123",
                ["EventType"] = "456"
            };

            var mapping = new TDEventMapping
            {
                JSON = json
            };

            // Act
            bool result = mapping.GenerateFields(out var errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors, Is.Empty);
            Assert.That(mapping.Fields, Has.Count.EqualTo(2));
            Assert.That(mapping.Fields[0].Path, Is.EqualTo("EventId"));
            Assert.That(mapping.Fields[0].Mapping, Is.EqualTo("123"));
            Assert.That(mapping.Fields[1].Path, Is.EqualTo("EventType"));
            Assert.That(mapping.Fields[1].Mapping, Is.EqualTo("456"));
        }

        [Test]
        public void GenerateFields_WithArrayProperty_ShouldReturnFalseAndAddError()
        {
            // Arrange
            var json = new JObject
            {
                ["EventId"] = "123",
                ["Tags"] = new JArray("tag1", "tag2")
            };

            var mapping = new TDEventMapping
            {
                JSON = json
            };

            // Act
            bool result = mapping.GenerateFields(out var errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("Tags"));
            Assert.That(errors[0], Does.Contain("array"));
        }

        [Test]
        public void GenerateFields_WithUnknownProperty_ShouldReturnFalseAndAddError()
        {
            // Arrange
            var json = new JObject
            {
                ["EventId"] = "123",
                ["UnknownProperty"] = "value"
            };

            var mapping = new TDEventMapping
            {
                JSON = json
            };

            // Act
            bool result = mapping.GenerateFields(out var errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("UnknownProperty"));
            Assert.That(errors[0], Does.Contain("not found"));
        }

        [Test]
        public void GenerateFields_WithNestedJsonObject_ShouldReturnTrueAndProcessNestedProperties()
        {
            // Arrange
            var json = new JObject
            {
                ["EventId"] = "123",
                ["Location"] = new JObject
                {
                    ["Name"] = "Test Location"
                }
            };

            var mapping = new TDEventMapping
            {
                JSON = json
            };

            // Act
            bool result = mapping.GenerateFields(out var errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors, Is.Empty);
            Assert.That(mapping.Fields, Has.Count.EqualTo(2));
            Assert.That(mapping.Fields[0].Path, Is.EqualTo("EventId"));
            Assert.That(mapping.Fields[0].Mapping, Is.EqualTo("123"));
            Assert.That(mapping.Fields[1].Path, Is.EqualTo("Location.Name"));
            Assert.That(mapping.Fields[1].Mapping, Is.EqualTo("Test Location"));
        }

        [Test]
        public void GenerateFields_WithExceptionThrown_ShouldReturnFalseAndAddError()
        {
            // Arrange
            // This is a null JObject which should cause an exception
            var mapping = new TDEventMapping
            {
                JSON = null!
            };

            // Act
            bool result = mapping.GenerateFields(out var errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors, Has.Count.GreaterThan(0));
        }
    }
}
