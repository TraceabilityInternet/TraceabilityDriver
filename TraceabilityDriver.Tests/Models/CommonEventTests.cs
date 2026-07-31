using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Tests.Models
{
    [TestFixture]
    public class CommonEventTests
    {

        [TestCase]
        public void CommonEvent_GetEventID_ReturnsHashURIScheme()
        {
            // generate a simple test id
            CommonEvent commonEvent = new CommonEvent()
            {
                EventType = "commissioning",
                EventKey = "TestEvent123"
            };

            // conver the test id to our hash uri
            Uri eventKey = commonEvent.GetEventKey();
            string uriString = eventKey.ToString();

            Assert.That(eventKey, Is.Not.Null);
            Assert.That(uriString, Is.Not.Empty);
        }
    }
}