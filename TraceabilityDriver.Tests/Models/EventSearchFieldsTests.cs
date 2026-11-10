using OpenTraceability.Interfaces;
using TraceabilityDriver.Models;
using TraceabilityDriver.Models.MongoDB;

namespace TraceabilityDriver.Tests.Models
{
    /// <summary>
    /// Unit tests for <see cref="EventSearchFields"/>.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class EventSearchFieldsTests
    {
        /// <summary>
        /// Initializes the OpenTraceability mappers once for the fixture.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            OpenTraceability.GDST.Setup.Initialize();
        }

        // FromEvent

        /// <summary>
        /// The business location is stored lowercased as the only bizLocation value.
        /// </summary>
        [Test]
        public void FromEvent_WithBizLocationOnly_SetsLowercasedBizLocationGLN()
        {
            // Arrange
            string gln = "urn:gdst:example.org:location:loc:Processor.PlantA";
            IEvent evt = BizLocationTestEventBuilder.CreateEvent(gln);

            // Act
            EventSearchFields fields = EventSearchFields.FromEvent(evt);

            // Assert
            Assert.That(fields.BizLocationGLN, Is.EqualTo(gln.ToLower()));
            Assert.That(fields.LocationGLNs, Is.EqualTo(new List<string> { gln.ToLower() }));
        }

        /// <summary>
        /// Source and destination locations are kept in LocationGLNs but never become the business location.
        /// </summary>
        [Test]
        public void FromEvent_WithSourceAndDestinationLocations_BizLocationGLNIsOnlyTheBizLocation()
        {
            // Arrange
            string bizLocation = "urn:epc:id:sgln:0614141.00001.0";
            string sourceLocation = "urn:epc:id:sgln:0614141.00002.0";
            string destinationLocation = "urn:epc:id:sgln:0614141.00003.0";
            IEvent evt = BizLocationTestEventBuilder.CreateEvent(bizLocation, sourceLocation, destinationLocation);

            // Act
            EventSearchFields fields = EventSearchFields.FromEvent(evt);

            // Assert
            Assert.That(fields.BizLocationGLN, Is.EqualTo(bizLocation));
            Assert.That(fields.LocationGLNs, Is.EqualTo(new List<string> { bizLocation, sourceLocation, destinationLocation }));
        }

        /// <summary>
        /// An event without a business location has no bizLocation even when it has a source location.
        /// </summary>
        [Test]
        public void FromEvent_NoBizLocationWithSourceLocation_BizLocationGLNIsNull()
        {
            // Arrange
            string sourceLocation = "urn:epc:id:sgln:0614141.00002.0";
            IEvent evt = BizLocationTestEventBuilder.CreateEvent(null, sourceLocation);

            // Act
            EventSearchFields fields = EventSearchFields.FromEvent(evt);

            // Assert
            Assert.That(fields.BizLocationGLN, Is.Null);
            Assert.That(fields.LocationGLNs, Is.EqualTo(new List<string> { sourceLocation }));
        }

        /// <summary>
        /// The business step and action are lowercased like every other indexed value.
        /// </summary>
        [Test]
        public void FromEvent_AnyEvent_LowercasesBizStepAndAction()
        {
            // Arrange
            IEvent evt = BizLocationTestEventBuilder.CreateEvent(null);

            // Act
            EventSearchFields fields = EventSearchFields.FromEvent(evt);

            // Assert
            Assert.That(fields.BizStep, Is.EqualTo(BizLocationTestEventBuilder.BizStep));
            Assert.That(fields.Action, Is.EqualTo("observe"));
        }

        /// <summary>
        /// A null event is rejected.
        /// </summary>
        [Test]
        public void FromEvent_NullEvent_ThrowsArgumentNullException()
        {
            // Act & Assert
            ArgumentNullException? exception = Assert.Throws<ArgumentNullException>(() => EventSearchFields.FromEvent(null!));

            Assert.That(exception!.ParamName, Is.EqualTo("evt"));
        }

        // ExtractBizLocationGLN

        /// <summary>
        /// The business location is recovered from stored event JSON, ignoring source and destination locations.
        /// </summary>
        [Test]
        public void ExtractBizLocationGLN_StoredEventWithBizLocation_ReturnsLowercasedBizLocation()
        {
            // Arrange
            string bizLocation = "urn:gdst:example.org:location:loc:Processor.PlantA";
            IEvent evt = BizLocationTestEventBuilder.CreateEvent(bizLocation, "urn:epc:id:sgln:0614141.00002.0", "urn:epc:id:sgln:0614141.00003.0");
            string eventJson = new EPCISEventDocument(evt).EventJson;

            // Act
            string? result = EventSearchFields.ExtractBizLocationGLN(eventJson);

            // Assert
            Assert.That(result, Is.EqualTo(bizLocation.ToLower()));
        }

        /// <summary>
        /// Stored event JSON without a business location yields null.
        /// </summary>
        [Test]
        public void ExtractBizLocationGLN_StoredEventWithoutBizLocation_ReturnsNull()
        {
            // Arrange
            IEvent evt = BizLocationTestEventBuilder.CreateEvent(null, "urn:epc:id:sgln:0614141.00002.0");
            string eventJson = new EPCISEventDocument(evt).EventJson;

            // Act
            string? result = EventSearchFields.ExtractBizLocationGLN(eventJson);

            // Assert
            Assert.That(result, Is.Null);
        }

        /// <summary>
        /// Malformed JSON throws so the caller can count and report the failure.
        /// </summary>
        [Test]
        public void ExtractBizLocationGLN_MalformedJson_Throws()
        {
            // Act & Assert
            Assert.That(() => EventSearchFields.ExtractBizLocationGLN("{ not json"), Throws.Exception);
        }
    }
}
