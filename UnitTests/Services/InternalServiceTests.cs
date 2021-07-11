using DirectoryService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriverService;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Clients;
using TraceabilityEngine.Databases.Mongo;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Models.Driver;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace UnitTests.Services
{
    [TestClass]
    public class InternalServiceTests
    {
        [TestMethod]
        public async Task DriverDB()
        {
            await UnitTests.ClearDatabases();
            ITDConfiguration configuration = await UnitTests.GetConfiguration("http://localhost:5000", "http://localhost:5001");
            using (ITEDriverDB driverDB = configuration.GetDB())
            {
                // create the account
                ITEDriverAccount account = new TEDriverAccount();
                account.Name = "Test Account #1";
                account.DigitalLinkURL = "www.google.com";

                // save the account
                await driverDB.SaveAccountAsync(account, configuration.URL);

                // check the account
                Assert.IsTrue(!IPGLN.IsNullOrEmpty(account.PGLN));
                Assert.IsTrue((account.ID > 0));
                Assert.IsTrue(!IDID.IsNullOrEmpty(account.DID));

                // load the account
                ITEDriverAccount loadedAccount = await driverDB.LoadAccountAsync(account.ID);
                Assert.IsNotNull(loadedAccount);
                Assert.AreEqual(account.ID, loadedAccount.ID);
                Assert.AreEqual(account.Name, loadedAccount.Name);
                Assert.AreEqual(account.DID.ToString(), loadedAccount.DID.ToString());
                Assert.AreEqual(account.PGLN, loadedAccount.PGLN);
                Assert.AreEqual(account.DigitalLinkURL, loadedAccount.DigitalLinkURL);

                // test that it will save correctly
                // it should find the existing account by the PGLN and grab it's ID and DID
                account.ID = 0;
                account.DID = null;
                await driverDB.SaveAccountAsync(account, configuration.URL);
                Assert.AreEqual(loadedAccount.DID.ToString(), account.DID.ToString());
                Assert.AreEqual(loadedAccount.ID, account.ID);

                // test changing the name of the account
                account.Name = account.Name = " (changed)";
                await driverDB.SaveAccountAsync(account, configuration.URL);
                loadedAccount = await driverDB.LoadAccountAsync(account.ID);
                Assert.AreEqual(loadedAccount.Name, account.Name);

                // create the trading partner
                ITEDriverTradingPartner tp = new TEDriverTradingPartner();
                tp.Name = "Test TP #1";
                tp.DigitalLinkURL = "www.google.com";
                tp.AccountID = account.ID;
                tp.DID = DID.GenerateNew();
                tp.PGLN = account.PGLN;

                // save the trading partner
                await driverDB.SaveTradingPartnerAsync(tp);
                Assert.IsTrue((tp.ID > 0));

                // load the trading partner
                ITEDriverTradingPartner loadedTP = await driverDB.LoadTradingPartnerAsync(tp.AccountID, tp.ID);
                Assert.IsNotNull(loadedTP);
                Assert.AreEqual(tp.ID, loadedTP.ID);
                Assert.AreEqual(tp.AccountID, loadedTP.AccountID);
                Assert.AreEqual(tp.Name, loadedTP.Name);
                Assert.AreEqual(tp.DID.ToString(), loadedTP.DID.ToString());
                Assert.AreEqual(tp.PGLN, loadedTP.PGLN);
                Assert.AreEqual(tp.DigitalLinkURL, loadedTP.DigitalLinkURL);

                // load the trading partner by pgln
                loadedTP = await driverDB.LoadTradingPartnerAsync(tp.AccountID, tp.ID);
                Assert.IsNotNull(loadedTP);
                Assert.AreEqual(tp.ID, loadedTP.ID);
                Assert.AreEqual(tp.AccountID, loadedTP.AccountID);
                Assert.AreEqual(tp.Name, loadedTP.Name);
                Assert.AreEqual(tp.DID.ToString(), loadedTP.DID.ToString());
                Assert.AreEqual(tp.PGLN, loadedTP.PGLN);
                Assert.AreEqual(tp.DigitalLinkURL, loadedTP.DigitalLinkURL);

                // test changing the name of the trading partner
                tp.Name = tp.Name = " (changed)";
                await driverDB.SaveTradingPartnerAsync(tp);
                loadedTP = await driverDB.LoadTradingPartnerAsync(tp.AccountID, tp.ID);
                Assert.AreEqual(loadedTP.Name, tp.Name);
            }
        }

        [TestMethod]
        public async Task AccountController()
        {
            string url = "http://localhost:9126";
            string directoryURL = "http://localhost:9125/";

            // create the account
            ITEDriverAccount account = new TEDriverAccount();
            account.Name = "Test Account #1";
            account.PGLN = IdentifierFactory.ParsePGLN($"urn:epc:id:sgln:08600031303.0.0");
            account.DigitalLinkURL = "www.google.com";

            // start the directory serivce
            UnitTests.StartDirectoryService(directoryURL);

            // start the controller
            ITDConfiguration configuration = await UnitTests.GetConfiguration(url, directoryURL);
            TraceabilityDriverService.Controllers.AccountController controller = new TraceabilityDriverService.Controllers.AccountController(configuration);

            // save the account
            await controller.Post(account);

            // load the account
            ITEDriverAccount loadedAccount = await controller.Get(account.ID.ToString());
            Assert.IsNotNull(loadedAccount);
            Assert.AreEqual(account.ID, loadedAccount.ID);
            Assert.AreEqual(account.Name, loadedAccount.Name);
            Assert.AreEqual(account.DID.ToString(), loadedAccount.DID.ToString());
            Assert.AreEqual(account.PGLN, loadedAccount.PGLN);
            Assert.AreEqual(account.DigitalLinkURL, loadedAccount.DigitalLinkURL);

            // load the account by the PGLN
            loadedAccount = await controller.Get(account.PGLN?.ToString());
            Assert.AreEqual(account.ID, loadedAccount.ID);
            Assert.AreEqual(account.Name, loadedAccount.Name);
            Assert.AreEqual(account.DID.ToString(), loadedAccount.DID.ToString());
            Assert.AreEqual(account.PGLN, loadedAccount.PGLN);
            Assert.AreEqual(account.DigitalLinkURL, loadedAccount.DigitalLinkURL);
        }

        [TestMethod]
        public async Task TradingPartnerController()
        {
            string url = "http://localhost:9127";
            string directoryURL = "http://localhost:9128/";
            await UnitTests.ClearDatabases(); // Checking if duplicates cause the MongoDB DID overwrite.

            // start the directory serivce
            UnitTests.StartDirectoryService(directoryURL);

            // start the controller
            ITDConfiguration configuration = await UnitTests.GetConfiguration(url, directoryURL);
            TraceabilityDriverService.Controllers.AccountController accountController = new TraceabilityDriverService.Controllers.AccountController(configuration);
            TraceabilityDriverService.Controllers.TradingPartnerController controller = new TraceabilityDriverService.Controllers.TradingPartnerController(configuration);

            // create the account
            ITEDriverAccount account = new TEDriverAccount();
            account.Name = "Test Account #1";
            account.PGLN = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.0.0");
            account.DigitalLinkURL = "www.google.com";
            await accountController.Post(account);

            // create a second account
            ITEDriverAccount account2 = new TEDriverAccount();
            account2.Name = "Test Account #2";
            account2.PGLN = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.2.0");
            account2.DigitalLinkURL = "www.google.com";
            await accountController.Post(account2);

            // create a third account
            ITEDriverAccount account3 = new TEDriverAccount();
            account3.Name = "Test Account #3";
            account3.PGLN = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.3.0");
            account3.DigitalLinkURL = "www.google.com";
            await accountController.Post(account3);

            // add second account as a trading partner to the first account
            IActionResult result = await controller.Post(account.ID, account2.PGLN?.ToString());
            Assert.IsTrue((result is AcceptedResult));
            AcceptedResult acceptedResult = result as AcceptedResult;
            ITEDriverTradingPartner tp = acceptedResult.Value as ITEDriverTradingPartner;
            Assert.IsNotNull(tp);

            // load the trading partner
            ITEDriverTradingPartner loadedTP = await controller.Get(account.ID, tp.ID);
            Assert.AreEqual(tp.ID, loadedTP.ID);
            Assert.AreEqual(tp.Name, loadedTP.Name);
            Assert.AreEqual(tp.DID.ToString(), loadedTP.DID.ToString());
            Assert.AreEqual(tp.PGLN, loadedTP.PGLN);
            Assert.AreEqual(tp.DigitalLinkURL, loadedTP.DigitalLinkURL);

            // delete the trading partner
            await controller.Delete(tp.AccountID, tp.ID);
            loadedTP = await controller.Get(account.ID, tp.ID);
            Assert.IsNull(loadedTP);

            // add third account as a manual trading partner to the first account
            tp = new TEDriverTradingPartner(account3);
            result = await controller.Post(account.ID, tp);
            Assert.IsTrue((result is AcceptedResult));
            acceptedResult = result as AcceptedResult;
            tp = acceptedResult.Value as ITEDriverTradingPartner;
            Assert.IsNotNull(tp);

            // load the trading partner
            loadedTP = await controller.Get(account.ID, tp.ID);
            Assert.AreEqual(tp.ID, loadedTP.ID);
            Assert.AreEqual(tp.Name, loadedTP.Name);
            Assert.AreEqual(tp.DID.ToString(), loadedTP.DID.ToString());
            Assert.AreEqual(tp.PGLN, loadedTP.PGLN);
            Assert.AreEqual(tp.DigitalLinkURL, loadedTP.DigitalLinkURL);

            // delete the trading partner
            await controller.Delete(tp.AccountID, tp.ID);
            loadedTP = await controller.Get(account.ID, tp.ID);
            Assert.IsNull(loadedTP);
        }

        [TestMethod]
        public async Task Client()
        {
            string url = "http://localhost:9127";
            string directoryURL = "http://localhost:9128/";

            // start the directory serivce
            DirectoryService.Program.Start(directoryURL, UnitTests.ConnectionString01);
            UnitTests.StartDirectoryService(directoryURL);

            ITDConfiguration configuration = await UnitTests.GetConfiguration(url, directoryURL);
            UnitTests.StartTraceabilityDriverService(configuration);

            using (ITEInternalClient client = TEClientFactory.InternalClient(url, configuration.APIKey))
            {
                // create the account
                ITEDriverAccount account = new TEDriverAccount();
                account.Name = "Test Account #1";
                account.DigitalLinkURL = "www.google.com";

                // add the account
                account = await client.SaveAccountAsync(account);

                // create a second account
                ITEDriverAccount account2 = new TEDriverAccount();
                account2.Name = "Test Account #2";
                account2.PGLN = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.2.0");
                account2.DigitalLinkURL = "www.google.com";
                account2 = await client.SaveAccountAsync(account2);

                // add second account as a trading partner to the first account
                ITEDriverTradingPartner tp = await client.AddTradingPartnerAsync(account.ID, account2.PGLN);
                Assert.IsNotNull(tp);

                // get the trading partner
                tp = await client.GetTradingPartnerAsync(tp.AccountID, tp.ID);
                Assert.IsNotNull(tp);

                // delete the trading partner
                await client.DeleteTradingPartnerAsync(tp.AccountID, tp.ID);

                // get the trading partner
                tp = await client.GetTradingPartnerAsync(tp.AccountID, tp.ID);
                Assert.IsNull(tp);
            }
        }
    }
}
