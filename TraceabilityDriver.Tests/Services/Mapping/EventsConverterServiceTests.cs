using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTraceability.GDST.Events;
using OpenTraceability.GDST.Events.KDEs;
using OpenTraceability.GDST.MasterData;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Events.KDEs;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Models.MasterData;
using TraceabilityDriver.Models;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;
using OpenTraceability.Utility;

namespace TraceabilityDriver.Tests.Services.Mapping
{
    [TestFixture]
    public class EventsConverterServiceTests
    {
        private Mock<ILogger<EventsConverterService>> _mockLogger;
        private EventsConverterService _service;
        
        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<EventsConverterService>>();
            _service = new EventsConverterService(_mockLogger.Object);
        }
        
        [Test]
        public async Task ConvertEventsAsync_WithValidEvents_ReturnsPopulatedEPCISDocument()
        {
            // Arrange
            var events = new List<CommonEvent>
            {
                CreateValidFishingEvent("event1")
            };
            
            // Act
            var result = await _service.ConvertEventsAsync(events);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Events, Has.Count.EqualTo(1));
            Assert.That(result.Events[0], Is.TypeOf<GDSTFishingEvent>());
        }
        
        [Test]
        public async Task ConvertEventsAsync_WithInvalidEvents_LogsErrorAndSkipsEvent()
        {
            // Arrange
            var events = new List<CommonEvent>
            {
                new CommonEvent { EventId = "invalid1", EventType = "GDSTFishingEvent" } // Missing products
            };
            
            // Act
            var result = await _service.ConvertEventsAsync(events);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Events, Is.Empty);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Event is not valid for conversion")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Test]
        public async Task ConvertEventsAsync_WithUnsupportedEventType_LogsErrorAndSkipsEvent()
        {
            // Arrange
            var events = new List<CommonEvent>
            {
                new CommonEvent { 
                    EventId = "unsupported1", 
                    EventType = "UnsupportedEventType",
                    Products = new List<CommonProduct> { CreateValidProduct() }
                }
            };
            
            // Act
            var result = await _service.ConvertEventsAsync(events);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Events, Is.Empty);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Event type not supported")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Test]
        public async Task ConvertEventsAsync_WhenExceptionOccurs_LogsErrorAndContinues()
        {
            // Arrange
            var events = new List<CommonEvent>
            {
                CreateValidFishingEvent("event1")
            };
            events.First().Products = null;
            
            // Act
            var result = await _service.ConvertEventsAsync(events);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Events, Is.Empty);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Test]
        public void IsEventValid_WithValidEvent_ReturnsTrue()
        {
            // Arrange
            var commonEvent = CreateValidFishingEvent("valid1");
            
            // Act
            var result = _service.IsEventValid(commonEvent, out string error);
            
            // Assert
            Assert.That(result, Is.True);
            Assert.That(error, Is.Empty);
        }
        
        [Test]
        public void IsEventValid_WithNullProducts_ReturnsFalse()
        {
            // Arrange
            var commonEvent = new CommonEvent { EventId = "event1", Products = null };
            
            // Act
            var result = _service.IsEventValid(commonEvent, out string error);
            
            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Is.EqualTo("Products is NULL."));
        }
        
        [Test]
        public void IsEventValid_WithEmptyProducts_ReturnsFalse()
        {
            // Arrange
            var commonEvent = new CommonEvent { EventId = "event1", Products = new List<CommonProduct>() };
            
            // Act
            var result = _service.IsEventValid(commonEvent, out string error);
            
            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Is.EqualTo("No products found on the event."));
        }
        
        [Test]
        public void IsEventValid_WithNullProductDefinition_ReturnsFalse()
        {
            // Arrange
            var commonEvent = new CommonEvent 
            { 
                EventId = "event1", 
                Products = new List<CommonProduct> 
                { 
                    new CommonProduct 
                    { 
                        ProductId = "product1",
                        ProductDefinition = null,
                        ProductType = EventProductType.Input
                    } 
                } 
            };
            
            // Act
            var result = _service.IsEventValid(commonEvent, out string error);
            
            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Is.EqualTo("Product definition is NULL."));
        }
        
        [Test]
        public void SetPartyMasterData_WithValidParty_ReturnsPartyAndAddsMasterData()
        {
            // Arrange
            var party = new CommonParty { OwnerId = "owner1", Name = "Test Party" };
            var doc = new EPCISDocument();
            
            // Act
            var result = _service.SetPartyMasterData(party, doc);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(doc.MasterData, Has.Count.EqualTo(1));
            Assert.That(doc.MasterData[0], Is.TypeOf<TradingParty>());
            var tradingParty = doc.MasterData[0] as TradingParty;
            Assert.That(tradingParty, Is.Not.Null);
            Assert.That(tradingParty.Name[0].Value, Is.EqualTo("Test Party"));
        }
        
        [Test]
        public void SetPartyMasterData_WithNullParty_ReturnsNull()
        {
            // Arrange
            CommonParty? party = null;
            var doc = new EPCISDocument();
            
            // Act
            var result = _service.SetPartyMasterData(party, doc);
            
            // Assert
            Assert.That(result, Is.Null);
            Assert.That(doc.MasterData, Is.Empty);
        }
        
        [Test]
        public void SetEventLocation_WithValidLocation_SetsLocationAndAddsMasterData()
        {
            // Arrange
            var location = new CommonLocation { 
                LocationId = "loc1", 
                Name = "Test Location", 
                OwnerId = "owner1",
                Country = Countries.FromAbbreviation("US")
            };
            var evt = new GDSTFishingEvent();
            var doc = new EPCISDocument();
            
            // Act
            _service.SetEventLocation(evt, location, doc);
            
            // Assert
            Assert.That(evt.Location, Is.Not.Null);
            Assert.That(evt.Location.GLN, Is.Not.Null);
            Assert.That(doc.MasterData, Has.Count.EqualTo(1));
            Assert.That(doc.MasterData[0], Is.TypeOf<GDSTLocation>());
            var locMasterData = doc.MasterData[0] as GDSTLocation;
            Assert.That(locMasterData, Is.Not.Null);    
            Assert.That(locMasterData.Name[0].Value, Is.EqualTo("Test Location"));
            Assert.That(locMasterData.Address.Country, Is.EqualTo(Countries.FromAbbreviation("US")));
        }
        
        [Test]
        public void SetProductMasterData_WithValidProductDefinition_ReturnsGTINAndAddsMasterData()
        {
            // Arrange
            var productDef = new CommonProductDefinition {
                ProductDefinitionId = "12345678901234",
                OwnerId = "owner1",
                ShortDescription = "Test Product",
                ProductForm = "Fresh",
                ScientificName = "Test Scientific Name"
            };
            var doc = new EPCISDocument();
            
            // Act
            var result = _service.SetProductMasterData(productDef, doc);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(doc.MasterData, Has.Count.EqualTo(1));
            Assert.That(doc.MasterData[0], Is.TypeOf<Tradeitem>());
            var tradeItem = doc.MasterData[0] as Tradeitem;
            Assert.That(tradeItem, Is.Not.Null);
            Assert.That(tradeItem.ShortDescription[0].Value, Is.EqualTo("Test Product"));
            Assert.That(tradeItem.TradeItemConditionCode, Is.EqualTo("Fresh"));
            Assert.That(tradeItem.FisherySpeciesScientificName[0], Is.EqualTo("Test Scientific Name"));
        }
        
        [Test]
        public void SetEventCertificates_WithValidCertificates_AddsCertificatesToList()
        {
            // Arrange
            var certificates = new CommonCertificates {
                FishingAuthorization = new CommonCertificate { Identifier = "auth123" }
            };
            var certList = new CertificationList();
            
            // Act
            _service.SetEventCertificates(certList, certificates);

            // Assert
            Assert.That(certList.Certificates, Has.Count.EqualTo(1));
            Assert.That(certList.Certificates[0].CertificateType, Is.EqualTo("urn:gdst:certType:fishingAuth"));
            Assert.That(certList.Certificates[0].Identification, Is.EqualTo("auth123"));
        }
        
        [Test]
        public void ConvertTo_GDSTFishingEvent_CreatesValidFishingEvent()
        {
            // Arrange
            var commonEvent = CreateValidFishingEvent("fishing1");
            var doc = new EPCISDocument();
            
            // Act
            _service.ConvertTo_GDSTFishingEvent(commonEvent, doc);
            
            // Assert
            Assert.That(doc.Events, Has.Count.EqualTo(1));
            Assert.That(doc.Events[0], Is.TypeOf<GDSTFishingEvent>());
            
            var fishingEvent = doc.Events[0] as GDSTFishingEvent;
            Assert.That(fishingEvent, Is.Not.Null);
            Assert.That(fishingEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(fishingEvent.ILMD.VesselCatchInformationList, Is.Not.Null);
            Assert.That(fishingEvent.ILMD.VesselCatchInformationList.Vessels, Has.Count.EqualTo(1));
            Assert.That(commonEvent.CatchInformation, Is.Not.Null);
            Assert.That(fishingEvent.ILMD.VesselCatchInformationList.Vessels[0].CatchArea, 
                Is.EqualTo(commonEvent.CatchInformation.CatchArea));
        }

        #region Helper Methods
        
        private CommonEvent CreateValidFishingEvent(string eventId)
        {
            return new CommonEvent
            {
                EventId = eventId,
                EventType = "GDSTFishingEvent",
                EventTime = DateTimeOffset.Now,
                InformationProvider = new CommonParty { OwnerId = "provider1", Name = "Provider" },
                ProductOwner = new CommonParty { OwnerId = "owner1", Name = "Owner" },
                Location = new CommonLocation 
                { 
                    LocationId = "loc1", 
                    Name = "Test Location",
                    OwnerId = "locowner1",
                    Country = Countries.FromAbbreviation("US")
                },
                CatchInformation = new CommonCatchInformation
                {
                    CatchArea = "FAO-27",
                    GearType = "Trawl",
                    GPSAvailable = true
                },
                Certificates = new CommonCertificates
                {
                    FishingAuthorization = new CommonCertificate { Identifier = "license123" }
                },
                Products = new List<CommonProduct> { CreateValidProduct() }
            };
        }
        
        private CommonProduct CreateValidProduct()
        {
            return new CommonProduct
            {
                ProductId = "product1",
                ProductType = EventProductType.Reference,
                LotNumber = "LOT123",
                Quantity = 100,
                UoM = "KGM",
                ProductDefinition = new CommonProductDefinition
                {
                    ProductDefinitionId = "12345678901234", // 14 digits for GTIN
                    OwnerId = "owner1",
                    ShortDescription = "Test Fish",
                    ProductForm = "Fresh",
                    ScientificName = "Testus fishus"
                }
            };
        }
        
        #endregion
    }
}
