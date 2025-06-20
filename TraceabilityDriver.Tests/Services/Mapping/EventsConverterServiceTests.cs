using Moq;
using Microsoft.Extensions.Logging;
using OpenTraceability.GDST.Events;
using OpenTraceability.GDST.MasterData;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Events.KDEs;
using OpenTraceability.Models.MasterData;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;
using OpenTraceability.Utility;
using OpenTraceability.MSC.Events;
using OpenTraceability.Mappers;

namespace TraceabilityDriver.Tests.Services.Mapping
{
    [TestFixture]
    public class EventsConverterServiceTests
    {
        private Mock<ILogger<EventsConverterService>> _mockLogger;
        private EventsConverterService _service;
        private EPCISDocument _document;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<EventsConverterService>>();
            _service = new EventsConverterService(_mockLogger.Object);
            _document = new EPCISDocument
            {
                EPCISVersion = EPCISVersion.V2,
                CreationDate = DateTimeOffset.UtcNow
            };
        }

        [Test]
        public async Task ConvertEventsAsync_WithValidEvents_ReturnsPopulatedEPCISDocument()
        {
            // Arrange
            var events = new List<CommonEvent>
            {
                CreateValidFishingEvent("event1"),
                CreateValidFeedMillObjectEvent("event2"),
                CreateValidFeedMillTransformationevent("event3"),
                CreateValidHatchingEvent("event4"),
                CreateValidShippingEvent("event5", "gdstshippingevent"),
                CreateValidReceiveEvent("event6", "gdstreceiveevent"),
                CreateValidFarmHarvestEvent("event7"),
                CreateValidProcessingEvent("event8", true),
                CreateValidShippingEvent("event9", "mscshippingevent"),
                CreateValidReceiveEvent("event10", "mscreceiveevent"),
                CreateValidStorageEvent("event11"),
                CreateValidComminglingEvent("event12"),
                CreateValidFarmHarvestObjectEvent("event13"),
                CreateValidProcessingEvent("event14"),
                CreateValidLandingEvent("event15"),
                CreateValidTransshipmentEvent("event16"),
                CreateValidAggregationEvent("event17"),
                CreateValidDisggregationEvent("event18")
            };

            // Act
            var result = await _service.ConvertEventsAsync(events);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Events, Has.Count.EqualTo(18));
            Assert.That(result.Events, Has.One.TypeOf<GDSTFishingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTFeedmillObjectEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTFeedmillTransformationEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTHatchingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTShippingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTReceiveEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTFarmHarvestEvent>());
            Assert.That(result.Events, Has.One.TypeOf<MSCProcessingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<MSCReceiveEvent>());
            Assert.That(result.Events, Has.One.TypeOf<MSCShippingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<MSCStorageEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTComminglingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTFarmHarvestObjectEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTProcessingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTLandingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTTransshipmentEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTAggregationEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTDisaggregationEvent>());

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(result);
            Assert.That(json, Is.Not.Null.Or.Empty);
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
                    Products = new List<CommonProduct> { CreateValidReferenceProduct() }
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
            var location = new CommonLocation
            {
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
            var productDef = new CommonProductDefinition
            {
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
            var certificates = new CommonCertificates
            {
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

            // Act
            _service.ConvertTo_GDSTFishingEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTFishingEvent>());

            var fishingEvent = _document.Events[0] as GDSTFishingEvent;
            Assert.That(fishingEvent, Is.Not.Null);
            Assert.That(fishingEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(fishingEvent.ILMD.VesselCatchInformationList, Is.Not.Null);
            Assert.That(fishingEvent.ILMD.VesselCatchInformationList.Vessels, Has.Count.EqualTo(1));
            Assert.That(commonEvent.CatchInformation, Is.Not.Null);
            Assert.That(fishingEvent.ILMD.VesselCatchInformationList.Vessels[0].CatchArea,
                Is.EqualTo(commonEvent.CatchInformation.CatchArea));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTFeedMillObjectEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidFeedMillObjectEvent("feedmill-object-1");

            // Act
            _service.ConvertTo_GDSTFeedMillObjectEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTFeedmillObjectEvent>());

            var feedmillObjectEvent = _document.Events[0] as GDSTFeedmillObjectEvent;
            Assert.That(feedmillObjectEvent, Is.Not.Null);
            Assert.That(feedmillObjectEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(feedmillObjectEvent.ILMD.ProteinSource, Is.EqualTo(commonEvent.ProteinSource));
            Assert.That(feedmillObjectEvent.ILMD.CertificationList, Is.Not.Null);
            Assert.That(feedmillObjectEvent.ILMD.CertificationList.Certificates, Has.Count.EqualTo(3));
            Assert.That(feedmillObjectEvent.HumanWelfarePolicy, Is.Not.Null);
            Assert.That(feedmillObjectEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTFeedMillTransformationEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidFeedMillTransformationevent("feedmill-transform-1");

            // Act
            _service.ConvertTo_GDSTFeedMillTransformationEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTFeedmillTransformationEvent>());

            var feedmillTransformEvent = _document.Events[0] as GDSTFeedmillTransformationEvent;
            Assert.That(feedmillTransformEvent, Is.Not.Null);
            Assert.That(feedmillTransformEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(feedmillTransformEvent.ILMD.ProteinSource, Is.EqualTo(commonEvent.ProteinSource));
            Assert.That(feedmillTransformEvent.ILMD.CertificationList, Is.Not.Null);
            Assert.That(feedmillTransformEvent.ILMD.CertificationList.Certificates, Has.Count.EqualTo(3));
            Assert.That(feedmillTransformEvent.HumanWelfarePolicy, Is.Not.Null);
            Assert.That(feedmillTransformEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTFarmHarvestEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidFarmHarvestEvent("farm-harvest-1");

            // Act
            _service.ConvertTo_GDSTFarmHarvestEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTFarmHarvestEvent>());

            var farmHarvestEvent = _document.Events[0] as GDSTFarmHarvestEvent;
            Assert.That(farmHarvestEvent, Is.Not.Null);
            Assert.That(farmHarvestEvent.EventTime, Is.EqualTo(commonEvent.EventTime));

            Assert.That(farmHarvestEvent.ILMD.AquacultureMethod, Is.EqualTo(commonEvent.AquacultureMethod));
            Assert.That(farmHarvestEvent.ILMD.ProductionMethodForFishAndSeafoodCode, Is.EqualTo(commonEvent.ProductionMethod));

            Assert.That(farmHarvestEvent.ILMD.CertificationList, Is.Not.Null);
            Assert.That(farmHarvestEvent.ILMD.CertificationList.Certificates, Has.Count.EqualTo(3));

            Assert.That(farmHarvestEvent.HumanWelfarePolicy, Is.Not.Null);
            Assert.That(farmHarvestEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTHatchingEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidHatchingEvent("hatching-1");

            // Act
            _service.ConvertTo_GDSTHatchingEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTHatchingEvent>());

            var harvestEvent = _document.Events[0] as GDSTHatchingEvent;
            Assert.That(harvestEvent, Is.Not.Null);
            Assert.That(harvestEvent.EventTime, Is.EqualTo(commonEvent.EventTime));

            Assert.That(harvestEvent.ILMD.BroodstockSource, Is.EqualTo(commonEvent.BroodStockSource));

            Assert.That(harvestEvent.ILMD.CertificationList, Is.Not.Null);
            Assert.That(harvestEvent.ILMD.CertificationList.Certificates, Has.Count.EqualTo(3));

            Assert.That(harvestEvent.HumanWelfarePolicy, Is.Not.Null);
            Assert.That(harvestEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTShippingEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidShippingEvent("gdst-shipping-1", "gdstshippingevent");

            // Act
            _service.ConvertTo_GDSTShippingEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTShippingEvent>());

            var shippingEvent = _document.Events[0] as GDSTShippingEvent;
            Assert.That(shippingEvent, Is.Not.Null);
            Assert.That(shippingEvent.EventTime, Is.EqualTo(commonEvent.EventTime));

            Assert.That(shippingEvent.SourceList, Is.Not.Null);
            Assert.That(shippingEvent.SourceList, Has.Count.EqualTo(2));

            Assert.That(shippingEvent.DestinationList, Is.Not.Null);
            Assert.That(shippingEvent.DestinationList, Has.Count.EqualTo(2));

            Assert.That(shippingEvent.CertificationList, Is.Not.Null);
            Assert.That(shippingEvent.CertificationList.Certificates, Has.Count.EqualTo(1));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTReceiveEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidReceiveEvent("gdst-receive-1", "gdstreceiveevent");

            // Act
            _service.ConvertTo_GDSTReceiveEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTReceiveEvent>());

            var receiveEvent = _document.Events[0] as GDSTReceiveEvent;
            Assert.That(receiveEvent, Is.Not.Null);
            Assert.That(receiveEvent.EventTime, Is.EqualTo(commonEvent.EventTime));

            Assert.That(receiveEvent.SourceList, Is.Not.Null);
            Assert.That(receiveEvent.SourceList, Has.Count.EqualTo(2));

            Assert.That(receiveEvent.DestinationList, Is.Not.Null);
            Assert.That(receiveEvent.DestinationList, Has.Count.EqualTo(2));

            Assert.That(receiveEvent.CertificationList, Is.Not.Null);
            Assert.That(receiveEvent.CertificationList.Certificates, Has.Count.EqualTo(1));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_MSCShippingEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidShippingEvent("msc-shipping-1", "mscshippingevent");

            // Act
            _service.ConvertTo_MSCShippingEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<MSCShippingEvent>());

            var shippingEvent = _document.Events[0] as MSCShippingEvent;
            Assert.That(shippingEvent, Is.Not.Null);
            Assert.That(shippingEvent.EventTime, Is.EqualTo(commonEvent.EventTime));

            Assert.That(shippingEvent.SourceList, Is.Not.Null);
            Assert.That(shippingEvent.SourceList, Has.Count.EqualTo(2));

            Assert.That(shippingEvent.DestinationList, Is.Not.Null);
            Assert.That(shippingEvent.DestinationList, Has.Count.EqualTo(2));

            Assert.That(shippingEvent.CertificationList, Is.Not.Null);
            Assert.That(shippingEvent.CertificationList.Certificates, Has.Count.EqualTo(1));

            Assert.That(shippingEvent.TransportNumber, Is.EqualTo(commonEvent.TransportNumber));
            Assert.That(shippingEvent.TransportProviderID, Is.EqualTo(commonEvent.TransportProviderID));
            Assert.That(shippingEvent.TransportType, Is.EqualTo(commonEvent.TransportType));
            Assert.That(shippingEvent.TransportVehicleID, Is.EqualTo(commonEvent.TransportVehicleID));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_MSCReceiveEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidShippingEvent("msc-receive-1", "mscreceiveevent");

            // Act
            _service.ConvertTo_MSCReceiveEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<MSCReceiveEvent>());

            var receiveEvent = _document.Events[0] as MSCReceiveEvent;
            Assert.That(receiveEvent, Is.Not.Null);
            Assert.That(receiveEvent.EventTime, Is.EqualTo(commonEvent.EventTime));

            Assert.That(receiveEvent.SourceList, Is.Not.Null);
            Assert.That(receiveEvent.SourceList, Has.Count.EqualTo(2));

            Assert.That(receiveEvent.DestinationList, Is.Not.Null);
            Assert.That(receiveEvent.DestinationList, Has.Count.EqualTo(2));

            Assert.That(receiveEvent.CertificationList, Is.Not.Null);
            Assert.That(receiveEvent.CertificationList.Certificates, Has.Count.EqualTo(1));

            Assert.That(receiveEvent.TransportNumber, Is.EqualTo(commonEvent.TransportNumber));
            Assert.That(receiveEvent.TransportProviderID, Is.EqualTo(commonEvent.TransportProviderID));
            Assert.That(receiveEvent.TransportType, Is.EqualTo(commonEvent.TransportType));
            Assert.That(receiveEvent.TransportVehicleID, Is.EqualTo(commonEvent.TransportVehicleID));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTProcessingEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidProcessingEvent("gdst-processing-1");

            // Act
            _service.ConvertTo_GDSTProcessingEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTProcessingEvent>());

            var processingEvent = _document.Events[0] as GDSTProcessingEvent;
            Assert.That(processingEvent, Is.Not.Null);
            Assert.That(processingEvent.EventTime, Is.EqualTo(commonEvent.EventTime));

            Assert.That(processingEvent.ILMD.CertificationList, Is.Not.Null);
            Assert.That(processingEvent.ILMD.CertificationList.Certificates, Has.Count.EqualTo(3));

            Assert.That(processingEvent.HumanWelfarePolicy, Is.Not.Null);
            Assert.That(processingEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_MSCProcessingEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidProcessingEvent("msc-processing-1", true);

            // Act
            _service.ConvertTo_MSCProcessingevent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<MSCProcessingEvent>());

            var processingEvent = _document.Events[0] as MSCProcessingEvent;
            Assert.That(processingEvent, Is.Not.Null);
            Assert.That(processingEvent.EventTime, Is.EqualTo(commonEvent.EventTime));

            Assert.That(processingEvent.ILMD.ProcessingType, Is.EqualTo(commonEvent.ProcessingType));

            Assert.That(processingEvent.ILMD.CertificationList, Is.Not.Null);
            Assert.That(processingEvent.ILMD.CertificationList.Certificates, Has.Count.EqualTo(3));

            Assert.That(processingEvent.HumanWelfarePolicy, Is.Not.Null);
            Assert.That(processingEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTTransshipmentEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidTransshipmentEvent("transshipment-event");

            // Act
            _service.ConvertTo_GDSTTransshippmentEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTTransshipmentEvent>());

            var transshipmentEvent = _document.Events[0] as GDSTTransshipmentEvent;
            Assert.That(transshipmentEvent, Is.Not.Null);
            Assert.That(transshipmentEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(transshipmentEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTLandingEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidLandingEvent("landing-event");

            // Act
            _service.ConvertTo_GDSTLandingEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTLandingEvent>());

            var landingEvent = _document.Events[0] as GDSTLandingEvent;
            Assert.That(landingEvent, Is.Not.Null);
            Assert.That(landingEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(landingEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_MSCStorageEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidStorageEvent("msc-storage-1");

            // Act
            _service.ConvertTo_MSCStorageEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<MSCStorageEvent>());

            var storageEvent = _document.Events[0] as MSCStorageEvent;
            Assert.That(storageEvent, Is.Not.Null);
            Assert.That(storageEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(storageEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTAggregationEvent_CreatesValidAggregationEvent()
        {
            // Arrange
            var commonEvent = CreateValidAggregationEvent("aggregation-event");

            // Act
            _service.ConvertTo_GDSTAggregationEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTAggregationEvent>());
            var aggregationEvent = _document.Events[0] as GDSTAggregationEvent;
            Assert.That(aggregationEvent, Is.Not.Null);
            Assert.That(aggregationEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(aggregationEvent.Products.Count, Is.EqualTo(2));
            Assert.That(aggregationEvent.Products[0].Type, Is.EqualTo(EventProductType.Parent));
            Assert.That(aggregationEvent.Products[1].Type, Is.EqualTo(EventProductType.Child));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }

        [Test]
        public void ConvertTo_GDSTDisaggregationEvent_CreatesValidDisaggregationEvent()
        {
            // Arrange
            var commonEvent = CreateValidDisggregationEvent("disaggregation-event");

            // Act
            _service.ConvertTo_GDSTDisaggregationEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTDisaggregationEvent>());
            var disaggregationEvent = _document.Events[0] as GDSTDisaggregationEvent;
            Assert.That(disaggregationEvent, Is.Not.Null);
            Assert.That(disaggregationEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(disaggregationEvent.Products.Count, Is.EqualTo(2));
            Assert.That(disaggregationEvent.Products[0].Type, Is.EqualTo(EventProductType.Parent));
            Assert.That(disaggregationEvent.Products[1].Type, Is.EqualTo(EventProductType.Child));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
        }
        #region Helper Methods

        private CommonEvent CreateValidEvent(string eventId)
        {
            return new CommonEvent
            {
                EventId = eventId,
                EventTime = DateTimeOffset.Now,
                InformationProvider = new CommonParty { OwnerId = "provider1", Name = "Provider" },
                ProductOwner = new CommonParty { OwnerId = "owner1", Name = "Owner" },
                HumanWelfarePolicy = "Policy123",
                Location = new CommonLocation
                {
                    LocationId = "loc1",
                    Name = "Test Location",
                    OwnerId = "locowner1",
                    Country = Countries.FromAbbreviation("US")
                },
            };
        }

        private CommonEvent CreateValidTransshipmentEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "GDSTTransshipmentEvent";
            
            commonEvent.Certificates = new CommonCertificates
            {
                TransshipmentAuthority = new CommonCertificate { Identifier = "transshipment-authority-123" },
                HarvestCertification = new CommonCertificate { Identifier = "harvest-certification-123" },
               HumanPolicyCertificate = new CommonCertificate { Identifier = "human-policy-123" }
            };

            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };
            return commonEvent;
        }

        private CommonEvent CreateValidFishingEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "GDSTFishingEvent";
            commonEvent.CatchInformation = new CommonCatchInformation
            {
                CatchArea = "FAO-27",
                GearType = "Trawl",
                GPSAvailable = true
            };
            commonEvent.Certificates = new CommonCertificates
            {
                FishingAuthorization = new CommonCertificate { Identifier = "license123" }
            };
            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };
            return commonEvent;
        }

        private CommonEvent CreateValidFeedMillObjectEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "GDSTFeedMillObjectEvent";
            commonEvent.ProteinSource = "Fishmeal";
            commonEvent.Certificates = new CommonCertificates
            {
                ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" },
                HumanPolicyCertificate = new CommonCertificate { Identifier = "human123" },
                HarvestCertification = new CommonCertificate { Identifier = "harvest123" }
            };
            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };
            return commonEvent;
        }

        private CommonEvent CreateValidFeedMillTransformationevent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "GDSTFeedMillTransformationEvent";
            commonEvent.ProteinSource = "Fishmeal";
            commonEvent.Certificates = new CommonCertificates
            {
                ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" },
                HumanPolicyCertificate = new CommonCertificate { Identifier = "human123" },
                HarvestCertification = new CommonCertificate { Identifier = "harvest123" }
            };
            commonEvent.Products = CreateValidTransformationProducts();
            return commonEvent;
        }

        private CommonEvent CreateValidHatchingEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "GDSTHatchingEvent";

            commonEvent.Certificates = new();
            commonEvent.Certificates.ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" };
            commonEvent.Certificates.HumanPolicyCertificate = new CommonCertificate { Identifier = "human123" };
            commonEvent.Certificates.HarvestCertification = new CommonCertificate { Identifier = "harvest123" };

            commonEvent.BroodStockSource = "Domestic";
            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };
            return commonEvent;
        }

        private CommonEvent CreateValidLandingEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "gdstlandingevent";

            commonEvent.HumanWelfarePolicy = "Policy123";
            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };
            return commonEvent;
        }

        private CommonEvent CreateValidStorageEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "mscstorageevent";

            commonEvent.HumanWelfarePolicy = "Policy123";
            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };
            return commonEvent;
        }

        private CommonEvent CreateValidShippingEvent(string eventId, string eventType)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = eventType;
            commonEvent.Certificates = new CommonCertificates
            {
                ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" },
            };
            commonEvent.Source = new CommonSource
            {
                Party = new CommonParty { OwnerId = "source1", Name = "Source Party" },
                Location = new CommonLocation
                {
                    LocationId = "sourceLoc1",
                    Name = "Source Location",
                    OwnerId = "sourceLocOwner1",
                    Country = Countries.FromAbbreviation("US")
                }
            };
            commonEvent.Destination = new CommonDestination
            {
                Party = new CommonParty { OwnerId = "dest1", Name = "Destination Party" },
                Location = new CommonLocation
                {
                    LocationId = "destLoc1",
                    Name = "Destination Location",
                    OwnerId = "destLocOwner1",
                    Country = Countries.FromAbbreviation("US")
                }
            };

            commonEvent.TransportNumber = "TR123";
            commonEvent.TransportProviderID = "Provider123";
            commonEvent.TransportType = "Truck";
            commonEvent.TransportVehicleID = "Truck123";

            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };

            return commonEvent;
        }

        private CommonEvent CreateValidReceiveEvent(string eventId, string eventType)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = eventType;
            commonEvent.Certificates = new CommonCertificates
            {
                ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" },
            };
            commonEvent.Source = new CommonSource
            {
                Party = new CommonParty { OwnerId = "source1", Name = "Source Party" },
                Location = new CommonLocation
                {
                    LocationId = "sourceLoc1",
                    Name = "Source Location",
                    OwnerId = "sourceLocOwner1",
                    Country = Countries.FromAbbreviation("US")
                }
            };
            commonEvent.Destination = new CommonDestination
            {
                Party = new CommonParty { OwnerId = "dest1", Name = "Destination Party" },
                Location = new CommonLocation
                {
                    LocationId = "destLoc1",
                    Name = "Destination Location",
                    OwnerId = "destLocOwner1",
                    Country = Countries.FromAbbreviation("US")
                }
            };

            commonEvent.TransportNumber = "TR123";
            commonEvent.TransportProviderID = "Provider123";
            commonEvent.TransportType = "Truck";
            commonEvent.TransportVehicleID = "Truck123";

            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };

            return commonEvent;
        }

        public CommonEvent CreateValidFarmHarvestObjectEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "GDSTFarmHarvestObjectEvent";
            commonEvent.Certificates = new CommonCertificates
            {
                ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" },
                HumanPolicyCertificate = new CommonCertificate { Identifier = "human123" },
                HarvestCertification = new CommonCertificate { Identifier = "harvest123" }
            };
            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };
            return commonEvent;
        }

        public CommonEvent CreateValidFarmHarvestEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "GDSTFarmHarvestEvent";
            commonEvent.Certificates = new CommonCertificates
            {
                ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" },
                HumanPolicyCertificate = new CommonCertificate { Identifier = "human123" },
                HarvestCertification = new CommonCertificate { Identifier = "harvest123" }
            };
            commonEvent.AquacultureMethod = "Cage and pen";
            commonEvent.ProductionMethod = "Aquaculture";

            commonEvent.Products = CreateValidTransformationProducts();

            return commonEvent;
        }

        public CommonEvent CreateValidAggregationEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "GDSTAggregationEvent";
            commonEvent.Products = CreateValidAggregationProducts();
            return commonEvent;
        }

        public CommonEvent CreateValidDisggregationEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "GDSTDisaggregationEvent";
            commonEvent.Products = CreateValidAggregationProducts();
            return commonEvent;
        }

        public CommonEvent CreateValidComminglingEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "GDSTComminglingEvent";
            commonEvent.Products = CreateValidTransformationProducts();
            return commonEvent;
        }

        public CommonEvent CreateValidProcessingEvent(string eventId, bool isMSCProcessingEvent = false)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            if (isMSCProcessingEvent)
            {
                commonEvent.EventType = "MSCProcessingEvent";
            }
            else
            {
                commonEvent.EventType = "GDSTProcessingEvent";
            }
            commonEvent.Certificates = new CommonCertificates
            {
                ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" },
                HumanPolicyCertificate = new CommonCertificate { Identifier = "human123" },
                HarvestCertification = new CommonCertificate { Identifier = "harvest123" }
            };

            if (isMSCProcessingEvent)
            {
                commonEvent.ProcessingType = "GENERAL";
            }

            commonEvent.Products = CreateValidTransformationProducts();

            return commonEvent;
        }

        private CommonProduct CreateValidReferenceProduct()
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

        private List<CommonProduct> CreateValidAggregationProducts()
        {
            return new List<CommonProduct>
            {
                new CommonProduct
                {
                    ProductId = "parentProduct",
                    ProductType = EventProductType.Parent,
                    SSCC = "sscc-123",
                },
                new CommonProduct
                {
                    ProductId = "childProduct1",
                    ProductType = EventProductType.Child,
                    LotNumber = "LOT123",
                    Quantity = 50,
                    UoM = "KGM",
                    ProductDefinition = new CommonProductDefinition
                    {
                        ProductDefinitionId = "12345678901234", // 14 digits for GTIN
                        OwnerId = "owner1",
                        ShortDescription = "Child Fish 1",
                        ProductForm = "Fresh",
                        ScientificName = "Testus fishus"
                    }
                },
            };
        }

        private List<CommonProduct> CreateValidTransformationProducts()
        {
            return new List<CommonProduct>
            {
                new CommonProduct
                {
                    ProductId = "product1",
                    ProductType = EventProductType.Input,
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
                },
                new CommonProduct
                {
                    ProductId = "product2",
                    ProductType = EventProductType.Output,
                    LotNumber = "LOT456",
                    Quantity = 50,
                    UoM = "KGM",
                    ProductDefinition = new CommonProductDefinition
                    {
                        ProductDefinitionId = "98765432109876", // 14 digits for GTIN
                        OwnerId = "owner2",
                        ShortDescription = "Processed Fish",
                        ProductForm = "Fillet",
                        ScientificName = "Testus fishus"
                    }
                }
            };

            #endregion
        }
    }
}
