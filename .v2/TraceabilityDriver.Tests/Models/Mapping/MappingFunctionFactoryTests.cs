using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriver.Services.Mapping;
using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver.Tests.Models.Mapping
{
    [TestFixture]
    public class MappingFunctionFactoryTests
    {
        private Mock<IServiceProvider> _mockServiceProvider;
        private Mock<IMappingFunction> _mockDictionaryFunc;
        private Mock<IMappingFunction> _mockLotNumberFunc;
        private Mock<ILogger<MappingFunctionFactory>> _mockLogger;
        private MappingFunctionFactory _factory;

        [SetUp]
        public void Setup()
        {
            _mockDictionaryFunc = new Mock<IMappingFunction>();
            _mockDictionaryFunc.Setup(f => f.Initialize());

            _mockLotNumberFunc = new Mock<IMappingFunction>();
            _mockLotNumberFunc.Setup(f => f.Initialize());

            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceProvider
                .As<IKeyedServiceProvider>()
                .Setup(sp => sp.GetKeyedService(typeof(IMappingFunction), "dictionary"))
                .Returns(_mockDictionaryFunc.Object);

            _mockServiceProvider
                .As<IKeyedServiceProvider>()
                .Setup(sp => sp.GetKeyedService(typeof(IMappingFunction), "lotnumber"))
                .Returns(_mockLotNumberFunc.Object);

            _mockLogger = new Mock<ILogger<MappingFunctionFactory>>();
            _factory = new MappingFunctionFactory(_mockServiceProvider.Object, _mockLogger.Object);
        }

        [Test]
        public void Create_CachesAndReturnsSameInstance_WhenCalledMultipleTimesWithSameFunctionName()
        {
            // Arrange

            // Act
            var function1 = _factory.Create("dictionary");
            var function2 = _factory.Create("dictionary");

            // Assert
            Assert.That(function1, Is.Not.Null);
            Assert.That(function2, Is.Not.Null);
            Assert.That(function2, Is.SameAs(function1), "Factory should return the same cached instance");
            _mockDictionaryFunc.Verify(f => f.Initialize(), Times.Once);
        }

        [Test]
        public void Create_NormalizesAndHandlesCaseInsensitiveFunctionNames()
        {
            // Arrange

            // Act
            var function1 = _factory.Create("Dictionary");
            var function2 = _factory.Create("  dictionary  ");
            var function3 = _factory.Create("DICTIONARY");

            // Assert
            Assert.That(function1, Is.Not.Null);
            Assert.That(function2, Is.SameAs(function1), "Factory should handle case-insensitive names");
            Assert.That(function3, Is.SameAs(function1), "Factory should handle case-insensitive names");
            _mockDictionaryFunc.Verify(f => f.Initialize(), Times.Once);
        }

        [Test]
        public void Create_ReturnsDictionaryFunction_WhenFunctionNameIsDictionary()
        {
            // Arrange

            // Act
            var function = _factory.Create("dictionary");

            // Assert
            Assert.That(function, Is.Not.Null);
            Assert.That(function, Is.EqualTo(_mockDictionaryFunc.Object));
        }

        [Test]
        public void Create_ReturnsLotNumberFunction_WhenFunctionNameIsLotNumber()
        {
            // Arrange

            // Act
            var function = _factory.Create("lotnumber");

            // Assert
            Assert.That(function, Is.Not.Null);
            Assert.That(function, Is.EqualTo(_mockLotNumberFunc.Object));
        }

        [Test]
        public void Create_ThrowsNotImplementedException_WhenFunctionNameIsUnknown()
        {
            // Arrange

            // Act & Assert
            var ex = Assert.Throws<NotImplementedException>(() => _factory.Create("unknownfunction"));
            Assert.That(ex.Message, Does.Contain("unknownfunction"));
        }
    }
}
