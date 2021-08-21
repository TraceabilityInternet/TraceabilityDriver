using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriverService.Controllers;
using TraceabilityDriverService.DB;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Clients;
using TraceabilityEngine.Databases.Mongo;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.DigitalLink;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Models.DigitalLink;
using TraceabilityEngine.Models.Driver;
using TraceabilityEngine.Models.Identifiers;

namespace UnitTests.Services
{
    [TestClass]
    public class DigitalLinkServiceTests
    {
        private async Task CreateLinks()
        {
            // create and save the digital links
            List<ITEDigitalLink> digitalLinks = new List<ITEDigitalLink>();
            digitalLinks.Add(new TEDigitalLink()
            {
                linkType = "gs1:masterData",
                link = "{url}/{account_id}/masterdata/tradeitem/{gtin}",
                identifier = "gtin"
            });
            digitalLinks.Add(new TEDigitalLink()
            {
                linkType = "gs1:masterData",
                link = "{url}/{account_id}/masterdata/location/{gln}",
                identifier = "gln"
            });
            digitalLinks.Add(new TEDigitalLink()
            {
                linkType = "gs1:masterData",
                link = "{url}/{account_id}/masterdata/tradingparty/{pgln}",
                identifier = "pgln"
            });
            digitalLinks.Add(new TEDigitalLink()
            {
                linkType = "gs1:epcis",
                link = "{url}/{account_id}/epcis",
                identifier = "epc"
            });

            await UnitTests.ClearDatabases();
            using (ITEDriverDB driverDB = new TEDriverDB(UnitTests.ConnectionString01))
            {
                foreach (var dl in digitalLinks)
                {
                    await driverDB.SaveDigitalLink(dl);
                }
            }
        }

        private async Task<ITEDriverAccount> CreateAccount(ITEInternalClient client, int number)
        {
            // create the account
            ITEDriverAccount account = new TEDriverAccount();
            account.Name = $"Test Account #{number}";
            account.PGLN = IdentifierFactory.ParsePGLN($"urn:epc:id:sgln:08600031303.0.{number}");
            account = await client.SaveAccountAsync(account);
            return account;
        }

        private async Task<ITEDriverTradingPartner> AddTradingPartner(ITEInternalClient client, ITEDriverAccount account, IPGLN pgln)
        {
            // add second account as a trading partner to the first account
            ITEDriverTradingPartner tp = await client.AddTradingPartnerAsync(account.ID, pgln);
            return tp;
        }

        [TestMethod]
        public async Task Links()
        {
            await CreateLinks();

            ITDConfiguration configuration = await UnitTests.GetConfiguration("http://localhost:4000", "http://localhost:4001");
            DigitalLinkController dlController = new DigitalLinkController(configuration);

            long accountID = 123;

            // trade items
            {
                string gtin = "00860003130308";
                IActionResult result1 = await dlController.GTIN(accountID, gtin, "gs1:masterData");
                Assert.IsTrue((result1 is OkObjectResult));
                List<ITEDigitalLink> links = (result1 as OkObjectResult).Value as List<ITEDigitalLink>;
                Assert.IsTrue((links.Count == 1));
                Assert.AreEqual($"{configuration.URL}/{accountID}/masterdata/tradeitem/{gtin}", links[0].link);
            }

            // locations
            {
                string gln = "860003130308"; // removed original leading 0
                IActionResult result2 = await dlController.GLN(accountID, gln, "gs1:masterData");
                Assert.IsTrue((result2 is OkObjectResult));
                List<ITEDigitalLink> links = (result2 as OkObjectResult).Value as List<ITEDigitalLink>;
                Assert.IsTrue((links.Count == 1));
                Assert.AreEqual($"{configuration.URL}/{accountID}/masterdata/location/{gln}", links[0].link);
            }

            // trading parties
            {
                string pgln = "860003130308"; // removed original leading 0 to make 12 digits
                IActionResult result3 = await dlController.PGLN(accountID, pgln, "gs1:masterData");
                Assert.IsTrue((result3 is OkObjectResult));
                List<ITEDigitalLink> links = (result3 as OkObjectResult).Value as List<ITEDigitalLink>;
                Assert.IsTrue((links.Count == 1));
                Assert.AreEqual($"{configuration.URL}/{accountID}/masterdata/tradingparty/{pgln}", links[0].link);
            }
        }

        [TestMethod]
        public async Task Events()
        {
            await CreateLinks();

            string directoryServiceURL = "http://localhost:1339";
            string solutionProviderURL = "http://localhost:1337";
            string traceDriveURL = "http://localhost:1338";

            // start the directory service URL
            UnitTests.StartDirectoryService(directoryServiceURL);

            // start the trace drive service
            ITDConfiguration config = await UnitTests.GetConfiguration(traceDriveURL, directoryServiceURL, solutionProviderURL);
            UnitTests.StartTraceabilityDriverService(config);

            // start the solution provider service
            UnitTests.StartTestSolutionProvider(solutionProviderURL);

            // instantiate the internal api client
            using (ITEInternalClient client = TEClientFactory.InternalClient(traceDriveURL, config.APIKey))
            {
                // create two accounts and add account #2 as a trading partner to account #1
                ITEDriverAccount account01 = await CreateAccount(client, 1);
                ITEDriverAccount account02 = await CreateAccount(client, 2);
                ITEDriverTradingPartner tp = await AddTradingPartner(client, account01, account02.PGLN);
                ITEDriverTradingPartner tp2 = await AddTradingPartner(client, account02, account01.PGLN);

                // request the events
                string json = await client.GetEventsAsync(account01.ID, tp.ID, "urn:epc:class:lgtin:08600031303.00.METAL");

                // this request returned the JSON in GS1 Web Vocab format
                // we are going to map that into the ITEProduct models
                var doc = config.Mapper.WriteEPCISData(json);
                Assert.IsNotNull(doc);
                Assert.AreEqual(doc.Events.Count, 2);
            }
        }

        [TestMethod]
        public async Task TradeItem()
        {
            await CreateLinks();

            string directoryServiceURL = "http://localhost:1359";
            string solutionProviderURL = "http://localhost:1357";
            string traceDriveURL = "http://localhost:1358";

            // start the directory service URL
            UnitTests.StartDirectoryService(directoryServiceURL);

            // start the trace drive service
            ITDConfiguration config = await UnitTests.GetConfiguration(traceDriveURL, directoryServiceURL, solutionProviderURL);
            UnitTests.StartTraceabilityDriverService(config);

            // start the solution provider service
            UnitTests.StartTestSolutionProvider(solutionProviderURL);

            // instantiate the internal api client
            using (ITEInternalClient client = TEClientFactory.InternalClient(traceDriveURL, config.APIKey))
            {
                // create two accounts and add account #2 as a trading partner to account #1
                ITEDriverAccount account01 = await CreateAccount(client, 1);
                ITEDriverAccount account02 = await CreateAccount(client, 2);
                ITEDriverTradingPartner tp = await AddTradingPartner(client, account01, account02.PGLN);
                ITEDriverTradingPartner tp2 = await AddTradingPartner(client, account02, account01.PGLN);

                // request the trade item
                string json = await client.GetTradeItemAsync(account01.ID, tp.ID, "urn:epc:idpat:sgtin:08600031303.00");

                // this request returned the JSON in GS1 Web Vocab format
                // we are going to map that into the ITEProduct models
                var products = config.Mapper.MapToGS1TradeItems(json);
                Assert.IsNotNull(products);
                Assert.AreEqual(products.Count, 1);
            }
        }

        [TestMethod]
        public async Task Location()
        {
            await CreateLinks();

            string directoryServiceURL = "http://localhost:1349";
            string solutionProviderURL = "http://localhost:1347";
            string traceDriveURL = "http://localhost:1348";

            // start the directory service URL
            UnitTests.StartDirectoryService(directoryServiceURL);

            // start the trace drive service
            ITDConfiguration config = await UnitTests.GetConfiguration(traceDriveURL, directoryServiceURL, solutionProviderURL);
            UnitTests.StartTraceabilityDriverService(config);


            // start the solution provider service
            UnitTests.StartTestSolutionProvider(solutionProviderURL);

            // instantiate the internal api client
            using (ITEInternalClient client = TEClientFactory.InternalClient(traceDriveURL, config.APIKey))
            {
                // create two accounts and add account #2 as a trading partner to account #1
                ITEDriverAccount account01 = await CreateAccount(client, 1);
                ITEDriverAccount account02 = await CreateAccount(client, 2);
                ITEDriverTradingPartner tp = await AddTradingPartner(client, account01, account02.PGLN);
                ITEDriverTradingPartner tp2 = await AddTradingPartner(client, account02, account01.PGLN);

                // request the location
                string json = await client.GetLocationAsync(account01.ID, tp.ID, "urn:epc:id:sgln:08600031303.0.0"); // EPCIS URN format is 13 digits?

                // this request returned the JSON in GS1 Web Vocab format
                // we are going to map that into the ITEProduct models
                var locations = config.Mapper.MapToGS1Locations(json);
                Assert.IsNotNull(locations);
                Assert.AreEqual(locations.Count, 1);
            }
        }

        [TestMethod]
        public async Task TradingPartner()
        {
            await CreateLinks();

            string directoryServiceURL = "http://localhost:1350";
            string solutionProviderURL = "http://localhost:1351";
            string traceDriveURL = "http://localhost:1352";


            // start the directory service URL
            UnitTests.StartDirectoryService(directoryServiceURL);

            // start the trace drive service
            ITDConfiguration config = await UnitTests.GetConfiguration(traceDriveURL, directoryServiceURL, solutionProviderURL);
            UnitTests.StartTraceabilityDriverService(config);


            // start the solution provider service
            UnitTests.StartTestSolutionProvider(solutionProviderURL);

            // instantiate the internal api client
            using (ITEInternalClient client = TEClientFactory.InternalClient(traceDriveURL, config.APIKey))
            {
                // create two accounts and add account #2 as a trading partner to account #1
                ITEDriverAccount account01 = await CreateAccount(client, 1);
                ITEDriverAccount account02 = await CreateAccount(client, 2);
                ITEDriverTradingPartner tp = await AddTradingPartner(client, account01, account02.PGLN);
                ITEDriverTradingPartner tp2 = await AddTradingPartner(client, account02, account01.PGLN);

                // request the location
                string json = await client.GetTradingPartyAsync(account01.ID, tp.ID, "urn:epc:id:sgln:08600031303.4.0"); // EPCIS URN format is 13 digits?

                // this request returned the JSON in GS1 Web Vocab format
                // we are going to map that into the ITEProduct models
                var tps = config.Mapper.MapToGS1TradingPartners(json);
                Assert.IsNotNull(tps);
                Assert.AreEqual(tps.Count, 1);
                Assert.IsFalse(string.IsNullOrWhiteSpace(tps.First().Name));
                Assert.IsNotNull(tps.First().PGLN);
            }
        }
    }
}
