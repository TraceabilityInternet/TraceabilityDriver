using DirectoryService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriverService.Controllers;
using TraceabilityEngine.Clients;
using TraceabilityEngine.Databases.Mongo;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Interfaces.Services.EPCIS;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Models.Driver;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace UnitTests.Services
{
    //[TestClass]
    //public class EPCISQueryTests
    //{
    //    [TestMethod]
    //    public async Task Controller()
    //    {
    //        string solutionProviderURL = "http://localhost:1337";
    //        string epc = "urn:epc:class:lgtin:08600031303.00.METAL";
    //        string connectionString = "mongodb://localhost";
    //        string urlTemplate = solutionProviderURL + "/xml/events/{epc}";
    //        string dll = @"C:\FOTFS\TraceabilityEngine\TestDriver\bin\Debug\net5.0\TestDriver.dll";
    //        string className = "TestDriver.XmlTestDriver";

    //        // build the configuration
    //        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
    //        Dictionary<string, string> settings = new Dictionary<string, string>();
    //        settings.Add("ConnectionString", connectionString);
    //        settings.Add("URLTemplate", urlTemplate);
    //        settings.Add("DriverDLLPath", dll);
    //        settings.Add("DriverClassName", className);
    //        settings.Add("RequiresAuthorization", "false");
    //        configBuilder.AddInMemoryCollection(settings);
    //        IConfiguration config = configBuilder.Build();

    //        // build the query controller
    //        EPCISQueryController controller = new EPCISQueryController(config);

    //        // start the Test Solution Provider Service
    //        TestSolutionProviderService.Program.Start(solutionProviderURL);

    //        // get the events
    //        IActionResult result = await controller.GetEvents(epc);
    //        Assert.IsTrue((result is OkObjectResult));
    //        string gs1Json = (result as OkObjectResult).Value.ToString();
    //        Assert.IsTrue(!string.IsNullOrWhiteSpace(gs1Json));
    //        ITEEventMapper mapper = new EPCISJsonMapper_2_0();
    //        List<ITEEvent> events = mapper.ConvertToEvents(gs1Json);
    //        Assert.IsTrue((events.Count == 2));
    //    }

    //    [TestMethod]
    //    public async Task Client()
    //    {
    //        string url = "http://localhost:1338";
    //        string solutionProviderURL = "http://localhost:1337";
    //        string epc = "urn:epc:class:lgtin:08600031303.00.METAL";
    //        string connectionString = "mongodb://localhost";
    //        string urlTemplate = solutionProviderURL + "/xml/events/{epc}";
    //        string dll = @"C:\FOTFS\TraceabilityEngine\TestDriver\bin\Debug\net5.0\TestDriver.dll";
    //        string className = "TestDriver.XmlTestDriver";

    //        string pgln1 = "urn:epc:id:sgln:08600031303.0";
    //        string pgln2 = "urn:epc:id:sgln:08600031303.2";
    //        IDID did = await SetupAccountAndTradingPartner(pgln1, pgln2);

    //        // build the configuration
    //        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
    //        Dictionary<string, string> settings = new Dictionary<string, string>();
    //        settings.Add("ConnectionString", connectionString);
    //        settings.Add("URLTemplate", urlTemplate);
    //        settings.Add("DriverDLLPath", dll);
    //        settings.Add("DriverClassName", className);
    //        settings.Add("RequiresAuthorization", "false");
    //        configBuilder.AddInMemoryCollection(settings);
    //        IConfiguration config = configBuilder.Build();

    //        // start the EPCIS Service
    //        TraceabilityDriverService.Program.Start(url, connectionString);

    //        // start the Test Solution Provider Service
    //        TestSolutionProviderService.Program.Start(solutionProviderURL);

    //        // get the events
    //        using (ITEEPCISQueryClient client = TEClientFactory.EPCISClient(url, did, pgln2, pgln1))
    //        {
    //            var events = await client.GetEvents(epc);
    //            Assert.IsTrue((events.Count == 2));
    //        }
    //    }

    //    private async Task<IDID> SetupAccountAndTradingPartner(string accountPGLN, string tradingPartnerPGLN)
    //    {
    //        string connectionString = "mongodb://localhost";
    //        string directoryURL = "http://localhost:9150";
    //        string internalURL = "http://localhost:9151/";
    //        string apiKey = Guid.NewGuid().ToString();
    //        IDID serviceProviderDID = DID.GenerateNew();

    //        using (ITEDocumentDB docDB = new TEMongoDatabase(connectionString, "TraceabilityDriver"))
    //        {
    //            await docDB.DropAsync("Account");
    //            await docDB.DropAsync("TradingPartner");
    //        }
    //        using (ITEDocumentDB docDB = new TEMongoDatabase(connectionString, "TEDirectory"))
    //        {
    //            await docDB.DropAsync("ServiceProvider");
    //            await docDB.DropAsync("Account");
    //        }

    //        ITEDirectoryServiceProvider serviceProvider = new TEDirectoryServiceProvider();
    //        serviceProvider.DID = serviceProviderDID;
    //        using (ITEDirectoryDB dirDB = DirectoryServiceUtil.GetDB(connectionString))
    //        {
    //            await dirDB.SaveServiceProviderAsync(serviceProvider);
    //        }

    //        DirectoryService.Program.Start(directoryURL, connectionString);
    //        TraceabilityDriverService.Program.Start(internalURL, connectionString);

    //        using (ITEInternalClient client = TEClientFactory.InternalClient(internalURL, apiKey))
    //        {
    //            // create the account
    //            ITEDriverAccount account = new TEDriverAccount();
    //            account.Name = "Test Account #1";
    //            account.DigitalLinkURL = "www.google.com";
    //            account.EPCISQueryURL = "www.google.com";
    //            account.DID = DID.GenerateNew();
    //            account.PGLN = IdentifierFactory.ParsePGLN(accountPGLN);

    //            // add the account
    //            account = await client.SaveAccountAsync(account);

    //            // create a second account
    //            ITEDriverAccount account2 = new TEDriverAccount();
    //            account2.Name = "Test Account #2";
    //            account2.PGLN = IdentifierFactory.ParsePGLN(tradingPartnerPGLN);
    //            account2.DigitalLinkURL = "www.google.com";
    //            account2.EPCISQueryURL = "www.google.com";
    //            account2 = await client.SaveAccountAsync(account2);

    //            // add second account as a trading partner to the first account
    //            ITEDriverTradingPartner tp = await client.AddTradingPartnerAsync(account.ID, account2.PGLN);
    //            Assert.IsNotNull(tp);

    //            // return the account DID
    //            return account2.DID;
    //        }
    //    }
    //}
}
