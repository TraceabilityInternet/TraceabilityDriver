using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.Security;

namespace UnitTests.Models.Directory
{
    [TestClass]
    public class DirectoryTests
    {
        [TestMethod]
        public void DirectoryAccount()
        {
            TEDirectoryAccount account = new TEDirectoryAccount();
            account.PublicDID = DIDFactory.GenerateNew();
            account.PGLN = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.0.0");
            account.DigitalLinkURL = "www.google.com";

            string jsonStr = account.ToJson();

            TEDirectoryAccount account2 = new TEDirectoryAccount();
            account2.FromJson(jsonStr);

            string jsonStr2 = account2.ToJson();

            Assert.AreEqual(account.ToJson(), account2.ToJson());
            Assert.AreEqual(jsonStr, jsonStr2);
        }

        [TestMethod]
        public void DirectoryNewAccount()
        {
            TEDirectoryNewAccount account = new TEDirectoryNewAccount();
            account.DID = DIDFactory.GenerateNew();
            account.PublicDID = account.DID.ToPublicDID();
            account.PGLN = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.0.0");
            account.DigitalLinkURL = "www.google.com";
            account.ServiceProviderDID = DIDFactory.GenerateNew();
            account.ServiceProviderPGLN = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.0.12");
            account.Sign();

            Assert.IsTrue(account.VerifySignature());

            string jsonStr = account.ToJson();

            TEDirectoryNewAccount account2 = new TEDirectoryNewAccount();
            account2.FromJson(jsonStr);

            string jsonStr2 = account2.ToJson();

            Assert.AreEqual(account.ToJson(), account2.ToJson());
            Assert.AreEqual(jsonStr, jsonStr2);
        }
    }
}
