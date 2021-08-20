using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace UnitTests.Util
{
    [TestClass]
    public class SecurityTests
    {
        [TestMethod]
        public void DID_v1()
        {
            IDID did1 = DIDFactory.GenerateNew();

            string data = "Hello Peter!";
            string nunce = Guid.NewGuid().ToString();
            ISimpleSignature signature = did1.Sign(data, nunce);

            bool isVerified = did1.Verify(data, nunce, signature.Signature);
            Assert.IsTrue(isVerified);

            isVerified = did1.Verify(data + "123", nunce, signature.Signature);
            Assert.IsFalse(isVerified);

            string didStr = did1.ToString();
            DID did2 = new DID();
            did2.Parse(didStr);

            isVerified = did1.Verify(signature);
            Assert.IsTrue(isVerified);

            isVerified = did1.Verify(data + "123", nunce, signature.Signature);
            Assert.IsFalse(isVerified);
        }

        [TestMethod]
        public void SimpleSignature_v1()
        {
            IDID did1 = DIDFactory.GenerateNew();

            string data = "Hello Peter!";
            string nunce = Guid.NewGuid().ToString();
            ISimpleSignature signature = did1.Sign(data, nunce);

            string signatureStr = signature.ToString();
            ISimpleSignature sigAfter = SimpleSignatureFactory.Parse(signatureStr);
            string sigAfterStr = sigAfter.ToString();

            Assert.AreEqual(signatureStr, sigAfterStr);
        }
    }
}
