using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TraceabilityDriver.Tests.Services.Mapping
{
    [TestFixture]
    public class EventsMergeByIdServiceTests
    {
        private Mock<ILogger<EventsMergeByIdService>> _mockLogger;
        private EventsMergeByIdService _service;
        private TDMapping _mapping;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<EventsMergeByIdService>>();
            _service = new EventsMergeByIdService(_mockLogger.Object);
            _mapping = new TDMapping
            {
                EventType = "TestEvent",
                Selectors = new List<TDMappingSelector>()
            };
        }

        [Test]
        public async Task MergeEventsAsync_EmptyEventsList_ReturnsEmptyList()
        {
            // Arrange
            var events = new List<CommonEvent>();

            // Act
            var result = await _service.MergeEventsAsync(_mapping, events);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task MergeEventsAsync_UniqueEventIds_ReturnsAllEvents()
        {
            // Arrange
            var events = new List<CommonEvent>
            {
                new CommonEvent { EventId = "1", EventType = "Type1" },
                new CommonEvent { EventId = "2", EventType = "Type2" },
                new CommonEvent { EventId = "3", EventType = "Type3" }
            };

            // Act
            var result = await _service.MergeEventsAsync(_mapping, events);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].EventId, Is.EqualTo("1"));
            Assert.That(result[1].EventId, Is.EqualTo("2"));
            Assert.That(result[2].EventId, Is.EqualTo("3"));
        }

        [Test]
        public async Task MergeEventsAsync_DuplicateEventIds_MergesDuplicates()
        {
            // Arrange
            var events = new List<CommonEvent>
            {
                new CommonEvent { EventId = "1", EventType = "Type1" },
                new CommonEvent { EventId = "2", EventType = "Type2" },
                new CommonEvent { EventId = "1", EventType = "Type1" }
            };

            // Act
            var result = await _service.MergeEventsAsync(_mapping, events);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Count(e => e.EventId == "1"), Is.EqualTo(1));
            Assert.That(result.Count(e => e.EventId == "2"), Is.EqualTo(1));
        }

        [Test]
        public async Task MergeEventsAsync_RespectsPriorityOrder()
        {
            // Arrange
            var firstEvent = new CommonEvent
            {
                EventId = "1",
                EventType = "Type1",
                Location = new CommonLocation { Name = "ABC Inc." }
            };

            var secondEvent = new CommonEvent
            {
                EventId = "1", 
                EventType = "Type1",
                Location = new CommonLocation { Name = "ABC" }
            };

            var events = new List<CommonEvent> { firstEvent, secondEvent };

            // Act
            var result = await _service.MergeEventsAsync(_mapping, events);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Location, Is.Not.Null);
            Assert.That(result[0].Location.Name, Is.EqualTo("ABC Inc."));
        }

        [Test]
        public async Task MergeEventsAsync_CombinesMultipleProperties()
        {
            // Arrange
            var firstEvent = new CommonEvent
            {
                EventId = "1",
                EventType = "Type1",
                EventTime = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Location = new CommonLocation { Name = "ABC Inc." }
            };

            var secondEvent = new CommonEvent
            {
                EventId = "1",
                EventType = "Type1",
                Location = new CommonLocation { 
                    LocationId = "LOC123", 
                    RegistrationNumber = "REG456"
                }
            };

            var events = new List<CommonEvent> { firstEvent, secondEvent };

            // Act
            var result = await _service.MergeEventsAsync(_mapping, events);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            
            var mergedEvent = result[0];
            Assert.That(mergedEvent.EventId, Is.EqualTo("1"));
            Assert.That(mergedEvent.EventType, Is.EqualTo("Type1"));
            Assert.That(mergedEvent.EventTime, Is.EqualTo(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            
            Assert.That(mergedEvent.Location, Is.Not.Null);
            Assert.That(mergedEvent.Location.Name, Is.EqualTo("ABC Inc."));
            Assert.That(mergedEvent.Location.LocationId, Is.EqualTo("LOC123"));
            Assert.That(mergedEvent.Location.RegistrationNumber, Is.EqualTo("REG456"));
        }

        [Test]
        public async Task MergeEventsAsync_MultipleDuplicateGroups_MergesEachGroupCorrectly()
        {
            // Arrange
            var events = new List<CommonEvent>
            {
                new CommonEvent { EventId = "1", EventType = "Type1", EventTime = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new CommonEvent { EventId = "2", EventType = "Type2" },
                new CommonEvent { EventId = "1", Location = new CommonLocation { Name = "Location1" } },
                new CommonEvent { EventId = "2", ProductOwner = new CommonParty { Name = "Owner2" } }
            };

            // Act
            var result = await _service.MergeEventsAsync(_mapping, events);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            
            var event1 = result.First(e => e.EventId == "1");
            Assert.That(event1.EventType, Is.EqualTo("Type1"));
            Assert.That(event1.EventTime, Is.EqualTo(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(event1.Location?.Name, Is.EqualTo("Location1"));

            var event2 = result.First(e => e.EventId == "2");
            Assert.That(event2.EventType, Is.EqualTo("Type2"));
            Assert.That(event2.ProductOwner?.Name, Is.EqualTo("Owner2"));
        }
    }
}
