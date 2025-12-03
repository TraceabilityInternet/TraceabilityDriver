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
                EventId = "TestEvent123"
            };

            // conver the test id to our hash uri
            Uri eventUri = commonEvent.GetEpcisEventId();
            string uriString = eventUri.ToString();

            // break apart the different key parts to check
            string[] parts = uriString.Split(';');
            Assert.That(parts.Length, Is.EqualTo(2), "URI should have 2 parts separated by ';'");

            string schemePart = parts[0];
            string valuePart = parts[1];

            // further split the hash part to get the version
            string[] hashParts = valuePart.Split('?');
            Assert.That(hashParts.Length, Is.EqualTo(2), "Hash part should have 2 parts separated by '?'");

            string hashPart = hashParts[0];
            string versionPart = hashParts[1];

            // assert each part is correct
            Assert.That(schemePart, Is.EqualTo("ni:///sha-256"));

            // check if the has is a valid 256 hash
            Assert.That(IsValidSha256Hex(hashPart), Is.True);

            // make sure we are formatting the version correctly.
            Assert.That(versionPart, Is.EqualTo("ver=CBV2.0"));
        }

        public bool IsValidSha256Hex(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // SHA-256 = 32 bytes = 64 hex characters
            if (value.Length != 64)
                return false;

            foreach (char c in value)
            {
                bool isHexDigit =
                    (c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f') ||
                    (c >= 'A' && c <= 'F');

                if (!isHexDigit)
                    return false;
            }

            return true;
        }
    }
}