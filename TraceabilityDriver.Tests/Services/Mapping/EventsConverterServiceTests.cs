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
                CreateValidCommissioningEvent("event1"),
                CreateValidDecommissioningEvent("event2"),
                CreateValidAggregationEvent("event3"),
                CreateValidDisaggregationEvent("event4"),
                CreateValidShippingEvent("event5", "shippingevent"),
                CreateValidReceiveEvent("event6", "receivingevent"),
                CreateValidTransformationEvent("event7"),
                CreateValidProcessingEvent("event8", "mscprocessingevent"),
                CreateValidShippingEvent("event9", "mscshippingevent"),
                CreateValidReceiveEvent("event10", "mscreceiveevent"),
                CreateValidStorageEvent("event11")
            };

            // Act
            var result = await _service.ConvertEventsAsync(events);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Events, Has.Count.EqualTo(11));
            Assert.That(result.Events, Has.One.TypeOf<GDSTCommissionEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTDecommissionEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTAggregationEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTDisaggregationEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTShippingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTReceivingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<GDSTTransformationEvent>());
            Assert.That(result.Events, Has.One.TypeOf<MSCProcessingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<MSCShippingEvent>());
            Assert.That(result.Events, Has.One.TypeOf<MSCReceiveEvent>());
            Assert.That(result.Events, Has.One.TypeOf<MSCStorageEvent>());

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(result);
            Assert.That(json, Is.Not.Null.Or.Empty);
        }

        [Test]
        public async Task ConvertEventsAsync_WithInvalidEvents_LogsErrorAndSkipsEvent()
        {
            // Arrange
            var events = new List<CommonEvent>
            {
                new CommonEvent { EventKey = "invalid1", EventType = "commissioningevent" } // Missing products
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
            // Arrange - the GDST 1.2 event types are no longer supported by the converter.
            var events = new List<CommonEvent>
            {
                new CommonEvent {
                    EventKey = "unsupported1",
                    EventType = "gdstfishingevent",
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
                CreateValidCommissioningEvent("event1")
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
            var commonEvent = CreateValidCommissioningEvent("valid1");

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
            var commonEvent = new CommonEvent { EventKey = "event1", Products = null };

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
            var commonEvent = new CommonEvent { EventKey = "event1", Products = new List<CommonProduct>() };

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
                EventKey = "event1",
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
                Country = Countries.FromAbbreviation("US"),
                LocationClassification = "vessel"
            };
            var evt = new GDSTCommissionEvent();
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
            Assert.That(locMasterData.LocationClassification, Has.Count.EqualTo(1));
            Assert.That(locMasterData.LocationClassification[0].Type, Is.EqualTo("gdst"));
            Assert.That(locMasterData.LocationClassification[0].Value, Is.EqualTo("vessel"));
        }

        [Test]
        public void SetEventLocation_WithoutClassification_LeavesClassificationsEmpty()
        {
            // Arrange
            var location = new CommonLocation
            {
                LocationId = "loc1",
                Name = "Test Location",
                OwnerId = "owner1",
                Country = Countries.FromAbbreviation("US")
            };
            var evt = new GDSTCommissionEvent();
            var doc = new EPCISDocument();

            // Act
            _service.SetEventLocation(evt, location, doc);

            // Assert
            var locMasterData = doc.MasterData[0] as GDSTLocation;
            Assert.That(locMasterData, Is.Not.Null);
            Assert.That(locMasterData.LocationClassification, Is.Empty);
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
                ScientificName = "Test Scientific Name",
                ProductClassification = "wildCaught"
            };
            var doc = new EPCISDocument();

            // Act
            var result = _service.SetProductMasterData(productDef, doc);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(doc.MasterData, Has.Count.EqualTo(1));
            Assert.That(doc.MasterData[0], Is.TypeOf<GDSTTradeItem>());
            var tradeItem = doc.MasterData[0] as GDSTTradeItem;
            Assert.That(tradeItem, Is.Not.Null);
            Assert.That(tradeItem.ShortDescription[0].Value, Is.EqualTo("Test Product"));
            Assert.That(tradeItem.TradeItemConditionCode, Is.EqualTo("Fresh"));
            Assert.That(tradeItem.FisherySpeciesScientificName[0], Is.EqualTo("Test Scientific Name"));
            Assert.That(tradeItem.ProductClassification, Has.Count.EqualTo(1));
            Assert.That(tradeItem.ProductClassification[0].Type, Is.EqualTo("gdst"));
            Assert.That(tradeItem.ProductClassification[0].Value, Is.EqualTo("wildCaught"));
        }

        [Test]
        public void SetProductMasterData_WithMultipleClassifications_AddsOneClassificationPerValue()
        {
            // Arrange - a comma-delimited classification must split into one entry per value.
            var productDef = new CommonProductDefinition
            {
                ProductDefinitionId = "12345678901234",
                OwnerId = "owner1",
                ShortDescription = "Test Product",
                ProductClassification = "seafood, processed"
            };
            var doc = new EPCISDocument();

            // Act
            _service.SetProductMasterData(productDef, doc);

            // Assert
            var tradeItem = doc.MasterData[0] as GDSTTradeItem;
            Assert.That(tradeItem, Is.Not.Null);
            Assert.That(tradeItem.ProductClassification, Has.Count.EqualTo(2));
            Assert.That(tradeItem.ProductClassification.Select(c => c.Value), Is.EquivalentTo(new[] { "seafood", "processed" }));
            Assert.That(tradeItem.ProductClassification.Select(c => c.Type), Is.All.EqualTo("gdst"));
        }

        [Test]
        public void SetProductMasterData_WithoutClassification_LeavesClassificationsEmpty()
        {
            // Arrange
            var productDef = new CommonProductDefinition
            {
                ProductDefinitionId = "12345678901234",
                OwnerId = "owner1",
                ShortDescription = "Test Product"
            };
            var doc = new EPCISDocument();

            // Act
            _service.SetProductMasterData(productDef, doc);

            // Assert
            var tradeItem = doc.MasterData[0] as GDSTTradeItem;
            Assert.That(tradeItem, Is.Not.Null);
            Assert.That(tradeItem.ProductClassification, Is.Empty);
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
        public void ConvertTo_GDSTCommissioningEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidCommissioningEvent("commissioning-1");

            // Act
            _service.ConvertTo_GDSTCommissioningEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTCommissionEvent>());

            var commissionEvent = _document.Events[0] as GDSTCommissionEvent;
            Assert.That(commissionEvent, Is.Not.Null);
            Assert.That(commissionEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(commissionEvent.Action, Is.EqualTo(EventAction.ADD));
            Assert.That(commissionEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));
            Assert.That(commissionEvent.ProductOwner, Is.Not.Null);
            Assert.That(commissionEvent.InformationProvider, Is.Not.Null);

            // The ILMD must carry every KDE the common event supplied.
            Assert.That(commonEvent.CatchInformation, Is.Not.Null);
            Assert.That(commissionEvent.ILMD.VesselCatchInformationList, Is.Not.Null);
            Assert.That(commissionEvent.ILMD.VesselCatchInformationList.Vessels, Has.Count.EqualTo(1));
            Assert.That(commissionEvent.ILMD.VesselCatchInformationList.Vessels[0].CatchArea, Is.EqualTo(commonEvent.CatchInformation.CatchArea));
            Assert.That(commissionEvent.ILMD.CertificationList, Is.Not.Null);
            Assert.That(commissionEvent.ILMD.CertificationList.Certificates, Has.Count.EqualTo(3));
            Assert.That(commissionEvent.ILMD.BroodstockSource, Is.EqualTo(commonEvent.BroodStockSource));
            Assert.That(commissionEvent.ILMD.AquacultureMethod, Is.EqualTo(commonEvent.AquacultureMethod));
            Assert.That(commissionEvent.ILMD.ProteinSource, Is.EqualTo(commonEvent.ProteinSource));
            Assert.That(commissionEvent.ILMD.ProductionMethodForFishAndSeafoodCode, Is.EqualTo(commonEvent.ProductionMethod));

            // The trade item master data must carry the product classification.
            var tradeItem = _document.MasterData.OfType<GDSTTradeItem>().FirstOrDefault();
            Assert.That(tradeItem, Is.Not.Null);
            Assert.That(tradeItem.ProductClassification.Select(c => c.Value), Does.Contain("wildCaught"));

            // The location master data must carry the location classification.
            var location = _document.MasterData.OfType<GDSTLocation>().FirstOrDefault();
            Assert.That(location, Is.Not.Null);
            Assert.That(location.LocationClassification.Select(c => c.Value), Does.Contain("vessel"));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
            Assert.That(json, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void ConvertTo_GDSTDecommissioningEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidDecommissioningEvent("decommissioning-1");

            // Act
            _service.ConvertTo_GDSTDecommissioningEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTDecommissionEvent>());

            var decommissionEvent = _document.Events[0] as GDSTDecommissionEvent;
            Assert.That(decommissionEvent, Is.Not.Null);
            Assert.That(decommissionEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(decommissionEvent.Action, Is.EqualTo(EventAction.DELETE));
            Assert.That(decommissionEvent.ProductOwner, Is.Not.Null);
            Assert.That(decommissionEvent.CertificationList, Is.Not.Null);
            Assert.That(decommissionEvent.CertificationList.Certificates, Has.Count.EqualTo(3));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
            Assert.That(json, Is.Not.Null.Or.Empty);
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
            Assert.That(aggregationEvent.Action, Is.EqualTo(EventAction.ADD));
            Assert.That(aggregationEvent.Products.Count, Is.EqualTo(2));
            Assert.That(aggregationEvent.Products[0].Type, Is.EqualTo(EventProductType.Parent));
            Assert.That(aggregationEvent.Products[1].Type, Is.EqualTo(EventProductType.Child));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
            Assert.That(json, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void ConvertTo_GDSTDisaggregationEvent_CreatesValidDisaggregationEvent()
        {
            // Arrange
            var commonEvent = CreateValidDisaggregationEvent("disaggregation-event");

            // Act
            _service.ConvertTo_GDSTDisaggregationEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTDisaggregationEvent>());
            var disaggregationEvent = _document.Events[0] as GDSTDisaggregationEvent;
            Assert.That(disaggregationEvent, Is.Not.Null);
            Assert.That(disaggregationEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(disaggregationEvent.Action, Is.EqualTo(EventAction.DELETE));
            Assert.That(disaggregationEvent.Products.Count, Is.EqualTo(2));
            Assert.That(disaggregationEvent.Products[0].Type, Is.EqualTo(EventProductType.Parent));
            Assert.That(disaggregationEvent.Products[1].Type, Is.EqualTo(EventProductType.Child));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
            Assert.That(json, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void ConvertTo_GDSTShippingEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidShippingEvent("gdst-shipping-1", "shippingevent");

            // Act
            _service.ConvertTo_GDSTShippingEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTShippingEvent>());

            var shippingEvent = _document.Events[0] as GDSTShippingEvent;
            Assert.That(shippingEvent, Is.Not.Null);
            Assert.That(shippingEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(shippingEvent.UnloadingPort, Is.EqualTo(commonEvent.UnloadingPort));

            Assert.That(shippingEvent.SourceList, Is.Not.Null);
            Assert.That(shippingEvent.SourceList, Has.Count.EqualTo(2));

            Assert.That(shippingEvent.DestinationList, Is.Not.Null);
            Assert.That(shippingEvent.DestinationList, Has.Count.EqualTo(2));

            Assert.That(shippingEvent.CertificationList, Is.Not.Null);
            Assert.That(shippingEvent.CertificationList.Certificates, Has.Count.EqualTo(1));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
            Assert.That(json, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void ConvertTo_GDSTReceivingEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidReceiveEvent("gdst-receiving-1", "receivingevent");

            // Act
            _service.ConvertTo_GDSTReceivingEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTReceivingEvent>());

            var receivingEvent = _document.Events[0] as GDSTReceivingEvent;
            Assert.That(receivingEvent, Is.Not.Null);
            Assert.That(receivingEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(receivingEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));
            Assert.That(receivingEvent.UnloadingPort, Is.EqualTo(commonEvent.UnloadingPort));

            Assert.That(receivingEvent.SourceList, Is.Not.Null);
            Assert.That(receivingEvent.SourceList, Has.Count.EqualTo(2));

            Assert.That(receivingEvent.DestinationList, Is.Not.Null);
            Assert.That(receivingEvent.DestinationList, Has.Count.EqualTo(2));

            Assert.That(receivingEvent.CertificationList, Is.Not.Null);
            Assert.That(receivingEvent.CertificationList.Certificates, Has.Count.EqualTo(1));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
            Assert.That(json, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void ConvertTo_GDSTTransformationEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidTransformationEvent("transformation-1");

            // Act
            _service.ConvertTo_GDSTTransformationEvent(commonEvent, _document);

            // Assert
            Assert.That(_document.Events, Has.Count.EqualTo(1));
            Assert.That(_document.Events[0], Is.TypeOf<GDSTTransformationEvent>());

            var transformationEvent = _document.Events[0] as GDSTTransformationEvent;
            Assert.That(transformationEvent, Is.Not.Null);
            Assert.That(transformationEvent.EventTime, Is.EqualTo(commonEvent.EventTime));
            Assert.That(transformationEvent.HumanWelfarePolicy, Is.EqualTo(commonEvent.HumanWelfarePolicy));
            Assert.That(transformationEvent.ProductOwner, Is.Not.Null);
            Assert.That(transformationEvent.Inputs, Is.Not.Empty);
            Assert.That(transformationEvent.Outputs, Is.Not.Empty);

            Assert.That(transformationEvent.ILMD, Is.Not.Null);
            Assert.That(transformationEvent.ILMD.CertificationList, Is.Not.Null);
            Assert.That(transformationEvent.ILMD.CertificationList.Certificates, Has.Count.EqualTo(3));

            // The output trade item carries a multi-value classification (seafood + processed).
            var outputGtin = commonEvent.Products!.First(p => p.ProductType == EventProductType.Output).ProductDefinition!.GetGTIN().ToString();
            var outputTradeItem = _document.MasterData.OfType<GDSTTradeItem>().First(t => t.GTIN!.ToString() == outputGtin);
            Assert.That(outputTradeItem.ProductClassification.Select(c => c.Value), Is.EquivalentTo(new[] { "seafood", "processed" }));

            string json = OpenTraceabilityMappers.EPCISDocument.JSON.Map(_document);
            Assert.That(json, Is.Not.Null.Or.Empty);
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
        public void ConvertTo_MSCProcessingEvent_CreatesValidEvent()
        {
            // Arrange
            var commonEvent = CreateValidProcessingEvent("msc-processing-1", "mscprocessingevent");

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

        #region Helper Methods

        private CommonEvent CreateValidEvent(string eventId)
        {
            return new CommonEvent
            {
                EventKey = eventId,
                EventTime = DateTimeOffset.Now,
                InformationProvider = new CommonParty { OwnerId = "provider1", Name = "Provider" },
                ProductOwner = new CommonParty { OwnerId = "owner1", Name = "Owner" },
                HumanWelfarePolicy = "Policy123",
                Location = new CommonLocation
                {
                    LocationId = "loc1",
                    Name = "Test Location",
                    OwnerId = "locowner1",
                    Country = Countries.FromAbbreviation("US"),
                    LocationClassification = "vessel"
                },
            };
        }

        private CommonEvent CreateValidCommissioningEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "commissioningevent";
            commonEvent.CatchInformation = new CommonCatchInformation
            {
                CatchArea = "FAO-27",
                GearType = "Trawl",
                GPSAvailable = true
            };
            commonEvent.Certificates = new CommonCertificates
            {
                ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" },
                HumanPolicyCertificate = new CommonCertificate { Identifier = "human123" },
                HarvestCertification = new CommonCertificate { Identifier = "harvest123" }
            };
            commonEvent.BroodStockSource = "Domestic";
            commonEvent.AquacultureMethod = "Cage and pen";
            commonEvent.ProteinSource = "Fishmeal";
            commonEvent.ProductionMethod = "MARINE_FISHERY";
            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };
            return commonEvent;
        }

        private CommonEvent CreateValidDecommissioningEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "decommissioningevent";
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

        private CommonEvent CreateValidAggregationEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "aggregationevent";
            commonEvent.Products = CreateValidAggregationProducts();
            return commonEvent;
        }

        private CommonEvent CreateValidDisaggregationEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "disaggregationevent";
            commonEvent.Products = CreateValidAggregationProducts();
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
            commonEvent.UnloadingPort = "Port of Seattle";

            commonEvent.Products = new List<CommonProduct>
            {
                CreateValidReferenceProduct()
            };

            return commonEvent;
        }

        private CommonEvent CreateValidReceiveEvent(string eventId, string eventType)
        {
            // Receiving events carry the same movement data as shipping events.
            return CreateValidShippingEvent(eventId, eventType);
        }

        private CommonEvent CreateValidTransformationEvent(string eventId)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = "transformationevent";
            commonEvent.Certificates = new CommonCertificates
            {
                ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" },
                HumanPolicyCertificate = new CommonCertificate { Identifier = "human123" },
                HarvestCertification = new CommonCertificate { Identifier = "harvest123" }
            };
            commonEvent.Products = CreateValidTransformationProducts();
            return commonEvent;
        }

        private CommonEvent CreateValidProcessingEvent(string eventId, string eventType)
        {
            CommonEvent commonEvent = CreateValidEvent(eventId);
            commonEvent.EventType = eventType;
            commonEvent.Certificates = new CommonCertificates
            {
                ChainOfCustodyCertification = new CommonCertificate { Identifier = "coc123" },
                HumanPolicyCertificate = new CommonCertificate { Identifier = "human123" },
                HarvestCertification = new CommonCertificate { Identifier = "harvest123" }
            };

            if (eventType?.ToLower() == "mscprocessingevent")
            {
                commonEvent.ProcessingType = "GENERAL";
            }

            commonEvent.Products = CreateValidTransformationProducts();

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
                    ScientificName = "Testus fishus",
                    ProductClassification = "wildCaught"
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
                        ScientificName = "Testus fishus",
                        ProductClassification = "wildCaught"
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
                        ScientificName = "Testus fishus",
                        ProductClassification = "wildCaught"
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
                        ScientificName = "Testus fishus",
                        ProductClassification = "seafood, processed"
                    }
                }
            };
        }

        #endregion
    }
}
