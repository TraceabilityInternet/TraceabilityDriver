using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Mapping;
using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver.Tests.Services.Mapping
{
    [TestFixture]
    public class EventTableMappingServiceTests
    {
        private Mock<ILogger<EventsTableMappingService>> _loggerMock;
        private Mock<IMappingFunctionFactory> _mappingFunctionFactoryMock;
        private EventsTableMappingService _service;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<EventsTableMappingService>>();
            _mappingFunctionFactoryMock = new Mock<IMappingFunctionFactory>();
            _service = new EventsTableMappingService(_loggerMock.Object, _mappingFunctionFactoryMock.Object);
        }

        [Test]
        public void MapEvents_WithValidDataTable_ReturnsListOfMappedEvents()
        {
            // Arrange
            var eventMapping = new TDEventMapping
            {
                Fields = new List<TDEventMappingField>
                    {
                        new TDEventMappingField("EventId", "$Id", typeof(CommonEvent).GetProperty("EventId")!),
                        new TDEventMappingField("Products[0].ProductType", "$ProductType", typeof(CommonProduct).GetProperty("ProductType")!),
                        new TDEventMappingField("Products[0].ProductId", "$ProductId", typeof(CommonProduct).GetProperty("ProductId")!),
                    }
            };

            var dataTable = new DataTable();
            dataTable.Columns.Add("Id", typeof(string));
            dataTable.Columns.Add("ProductType", typeof(string));
            dataTable.Columns.Add("ProductId", typeof(string));
            dataTable.Rows.Add("Event1", "Input", "product_1");
            dataTable.Rows.Add("Event2", "Output", "product_2");

            // Act
            var result = _service.MapEvents(eventMapping, dataTable, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First().Products?.First().ProductType == OpenTraceability.Models.Events.EventProductType.Input);
            Assert.That(result.Skip(1).First().Products?.First().ProductType == OpenTraceability.Models.Events.EventProductType.Output);
        }

        [Test]
        public void MapDataRowToCommonEvent_WithValidMapping_ReturnsPopulatedCommonEvent()
        {
            // Arrange
            var eventMapping = new TDEventMapping
            {
                Fields = new List<TDEventMappingField>
                {
                    new TDEventMappingField("EventId", "$Id", typeof(CommonEvent).GetProperty("EventId")!)
                }
            };

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Id", typeof(string));
            DataRow row = dataTable.Rows.Add("Event1");

            // Act
            var result = _service.MapDataRowToCommonEvent(eventMapping, row);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void MapEventFieldToCommonEvent_WithStaticValue_SetsPropertyCorrectly()
        {
            // Arrange
            var eventMapping = new TDEventMapping
            {
                Fields = new List<TDEventMappingField>
                {
                    new TDEventMappingField("EventId", "!123", typeof(CommonEvent).GetProperty("EventId")!)
                }
            };

            CommonEvent commonEvent = new CommonEvent();

            var dataRow = new DataTable().NewRow();

            // Act
            _service.MapEventFieldToCommonEvent(eventMapping.Fields.First(), dataRow, commonEvent);

            // Assert
            Assert.That(commonEvent.EventId, Is.EqualTo("123"));
        }

        [Test]
        public void MapEventFieldToCommonEvent_WithFunctionValue_SetsPropertyCorrectly()
        {
            // Arrange
            var commonEvent = new CommonEvent();

            var eventMapping = new TDEventMapping
            {
                Fields = new List<TDEventMappingField>
                {
                    new TDEventMappingField("EventId", "TestFunction(FunctionResult)", typeof(CommonEvent).GetProperty("EventId")!)
                }
            };

            var mockFunction = new Mock<IMappingFunction>();
            mockFunction.Setup(f => f.Execute(It.IsAny<List<string?>>())).Returns("FunctionResult");

            _mappingFunctionFactoryMock.Setup(f => f.Create("TestFunction")).Returns(mockFunction.Object);

            var dataRow = new DataTable().NewRow();

            // Act
            _service.MapEventFieldToCommonEvent(eventMapping.Fields.First(), dataRow, commonEvent);

            // Assert
            Assert.That(commonEvent.EventId, Is.EqualTo("FunctionResult"));
        }

        [Test]
        public void GetTargetObject_WithValidPath_ReturnsCorrectObject()
        {
            // Arrange
            var commonEvent = new CommonEvent
            {
                Location = new CommonLocation()
                {
                    Name = "main street"
                }
            };

            // Act
            var result = _service.GetTargetObject(commonEvent, "Location.Name");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<CommonLocation>());
        }

        [Test]
        public void GetTargetObject_WithInvalidPath_ThrowsException()
        {
            // Arrange
            var commonEvent = new CommonEvent
            {
                Location = new CommonLocation()
                {
                    Name = "main street"
                }
            };

            // Act & Assert
            Assert.That(() => _service.GetTargetObject(commonEvent, "InvalidObj.Name"),
                Throws.Exception.TypeOf<Exception>()
                .With.Message.Contains("Property not found: InvalidObj on CommonEvent"));
        }

        [Test]
        public void GetTargetObject_WithArrayPath()
        {
            // Arrange
            var commonEvent = new CommonEvent
            {
                Products = new List<CommonProduct>
                {
                    new CommonProduct { LotNumber = "Product1" },
                    new CommonProduct { LotNumber = "Product2" }
                }
            };

            // Act
            var result = _service.GetTargetObject(commonEvent, "Products[1].LotNumber");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(commonEvent.Products[1]));
        }

        [Test]
        public void TryToConvertValue_WithStringConversion_ReturnsOriginalString()
        {
            // Arrange
            string testValue = "TestString";

            // Act
            var result = _service.TryToConvertValue(testValue, typeof(string));

            // Assert
            Assert.That(result, Is.EqualTo(testValue));
        }

        [Test]
        public void TryToConvertValue_WithIntConversion_ReturnsConvertedInt()
        {
            // Arrange
            string testValue = "123";

            // Act
            var result = _service.TryToConvertValue(testValue, typeof(int));

            // Assert
            Assert.That(result, Is.EqualTo(123));
        }

        [Test]
        public void TryToConvertValue_WithInvalidConversion_ReturnsNull()
        {
            // Arrange
            string testValue = "NotAnInt";

            // Act
            var result = _service.TryToConvertValue(testValue, typeof(int));

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryToSetValue_WithValidValue_SetsPropertyCorrectly()
        {
            // Arrange
            var commonEvent = new CommonEvent();
            var propertyInfo = typeof(CommonEvent).GetProperty("EventId")!;
            string value = "TestEvent";

            // Act
            _service.TryToSetValue(commonEvent, propertyInfo, value);

            // Assert
            Assert.That(commonEvent.EventId, Is.EqualTo(value));
        }

        [Test]
        public void GetFunctionReturnValue_CallsCorrectFunction_ReturnsExpectedValue()
        {
            // Arrange
            string functionName = "TestFunction";
            var parameters = new List<string?> { "param1", "param2" };

            var mockFunction = new Mock<IMappingFunction>();
            mockFunction.Setup(f => f.Execute(parameters)).Returns("FunctionResult");

            _mappingFunctionFactoryMock.Setup(f => f.Create(functionName)).Returns(mockFunction.Object);

            // Act
            var result = _service.GetFunctionReturnValue(functionName, parameters);

            // Assert
            Assert.That(result, Is.EqualTo("FunctionResult"));
            _mappingFunctionFactoryMock.Verify(f => f.Create(functionName), Times.Once);
            mockFunction.Verify(f => f.Execute(parameters), Times.Once);
        }
    }
}
