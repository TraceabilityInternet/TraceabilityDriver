using DirectoryService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Clients;
using TraceabilityEngine.Databases.Mongo;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Models.Driver;
using TraceabilityEngine.Util.Security;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Models.DigitalLink;
using TraceabilityEngine.Models.DigitalLink;
using TraceabilityEngine.Service.Util.DB;
using TraceabilityEngine.Interfaces.Models.Events;

namespace UnitTests.Services
{
    [TestClass]
    public class AdvancedTests
    {
        /// <summary>
        /// This covers the situation where two accounts will exchange data. One account will use an XML format, while the other account will use a JSON 
        /// format. This will host two instances of the traceability driver.
        /// </summary>
        [TestMethod]
        public async Task With_DirectoryService()
        {
            await UnitTests.ClearDatabases();
            await CreateLinks();

            // setup the directory service
            string td01URL = "http://localhost:1367";
            string td02URL = "http://localhost:1368";
            string directoryURL = "http://localhost:1369";
            string solutionProviderURL = "http://localhost:1360";

            UnitTests.StartDirectoryService(directoryURL);

            ITDConfiguration config01 = await UnitTests.GetConfiguration(td01URL, directoryURL, solutionProviderURL);
            ITDConfiguration config02 = await UnitTests.GetConfiguration(td02URL, directoryURL, solutionProviderURL);
            UnitTests.StartTraceabilityDriverService(config01);
            UnitTests.StartTraceabilityDriverService(config02);

            UnitTests.StartTestSolutionProvider(solutionProviderURL);

            using (ITEInternalClient td01Client = TEClientFactory.InternalClient(td01URL, config01.APIKey))
            using (ITEInternalClient td02Client = TEClientFactory.InternalClient(td02URL, config02.APIKey))
            {
                // add account #1 to traceability #1
                ITEDriverAccount account01 = new TEDriverAccount();
                account01.Name = "Test Account #1";
                account01.PGLN = new PGLN("urn:epc:id:sgln:08600031303.0.1");
                account01 = await td01Client.SaveAccountAsync(account01);

                // add account #2 to traceability #2
                ITEDriverAccount account02 = new TEDriverAccount();
                account02.Name = "Test Account #2";
                account02.PGLN = new PGLN("urn:epc:id:sgln:08600031303.0.8"); // Double check if they can be the same
                account02 = await td02Client.SaveAccountAsync(account02); // changing client so account 2 is saved with a diff client.

                // add account #2 as trading partner to account #1
                ITEDriverTradingPartner tp01 = await AddTradingPartner(td01Client, account01, account02.PGLN);

                // add account #1 as trading partner to account #2
                ITEDriverTradingPartner tp02 = await AddTradingPartner(td02Client, account02, account01.PGLN);

                // account #1 requests event data from account #2
                string json = await td01Client.GetEventsAsync(account01.ID, tp01.ID, "urn:epc:class:lgtin:08600031303.00.METAL"); // EPC from first entry in Events.xml
                var events = config01.Mapper.MapToGS1Events(json, null);
                Assert.IsNotNull(events);
                Assert.AreEqual(events.Count, 2);

                //// account #1 requests master data from account #2
                json = await td01Client.GetTradeItemAsync(account01.ID, tp01.ID, "urn:epc:idpat:sgtin:08600031303.00");
                var products = config01.Mapper.MapToGS1TradeItems(json);
                Assert.IsNotNull(products);
                Assert.AreEqual(products.Count, 1);
            }
        }

        /// <summary>
        /// This covers the situation where two accounts will exchange data. One account will use an XML format, while the other account will use a JSON 
        /// format. This will host two instances of the traceability driver.
        /// </summary>
        [TestMethod]
        public async Task Without_DirectoryService()
        {
            await UnitTests.ClearDatabases();
            await CreateLinks();

            // setup the directory service
            string td01URL = "http://localhost:1367";
            string td02URL = "http://localhost:1368";
            string solutionProviderURL = "http://localhost:1360";

            ITDConfiguration config01 = await UnitTests.GetConfiguration(td01URL, null, solutionProviderURL);
            ITDConfiguration config02 = await UnitTests.GetConfiguration(td02URL, null, solutionProviderURL);
            UnitTests.StartTraceabilityDriverService(config01);
            UnitTests.StartTraceabilityDriverService(config02);

            UnitTests.StartTestSolutionProvider(solutionProviderURL);

            using (ITEInternalClient td01Client = TEClientFactory.InternalClient(td01URL, config01.APIKey))
            using (ITEInternalClient td02Client = TEClientFactory.InternalClient(td02URL, config02.APIKey))
            {
                // add account #1 to traceability #1
                ITEDriverAccount account01 = new TEDriverAccount();
                account01.Name = "Test Account #1";
                account01.PGLN = new PGLN("urn:epc:id:sgln:08600031303.0.1");
                account01 = await td01Client.SaveAccountAsync(account01);

                // add account #2 to traceability #2
                ITEDriverAccount account02 = new TEDriverAccount();
                account02.Name = "Test Account #2";
                account02.PGLN = new PGLN("urn:epc:id:sgln:08600031303.0.8"); // Double check if they can be the same
                account02 = await td02Client.SaveAccountAsync(account02); // changing client so account 2 is saved with a diff client.

                // add account #2 as trading partner to account #1
                ITEDriverTradingPartner tp01 = await AddTradingPartner(td01Client, account01, account02);

                // add account #1 as trading partner to account #2
                ITEDriverTradingPartner tp02 = await AddTradingPartner(td02Client, account02, account01);

                // account #1 requests event data from account #2
                string json = await td01Client.GetEventsAsync(account01.ID, tp01.ID, "urn:epc:class:lgtin:08600031303.00.METAL"); // EPC from first entry in Events.xml
                var events = config01.Mapper.MapToGS1Events(json, null);
                Assert.IsNotNull(events);
                Assert.AreEqual(events.Count, 2);

                //// account #1 requests master data from account #2
                json = await td01Client.GetTradeItemAsync(account01.ID, tp01.ID, "urn:epc:idpat:sgtin:08600031303.00");
                var products = config01.Mapper.MapToGS1TradeItems(json);
                Assert.IsNotNull(products);
                Assert.AreEqual(products.Count, 1);
            }
        }

        private async Task<ITEDriverTradingPartner> AddTradingPartner(ITEInternalClient client, ITEDriverAccount account, IPGLN pgln)
        {
            // add second account as a trading partner to the first account
            ITEDriverTradingPartner tp = await client.AddTradingPartnerAsync(account.ID, pgln);
            return tp;
        }

        private async Task<ITEDriverTradingPartner> AddTradingPartner(ITEInternalClient client, ITEDriverAccount account, ITEDriverAccount tpAccount)
        {
            // add second account as a trading partner to the first account
            ITEDriverTradingPartner tp = new TEDriverTradingPartner(tpAccount);
            tp = await client.AddTradingPartnerManuallyAsync(account.ID, tp);
            return tp;
        }

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
    }
}
