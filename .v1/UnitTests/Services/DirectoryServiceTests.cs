using DirectoryService;
using DirectoryService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Clients;
using TraceabilityEngine.Databases.Mongo;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.Security;

namespace UnitTests.Services
{
    [TestClass]
    public class DirectoryServiceTests
    {
        [TestMethod]
        public async Task Controller()
        {
            string connectionString = "mongodb://localhost";
            using (ITEDocumentDB docDB = new TEMongoDatabase(connectionString, "TEDirectory"))
            {
                await docDB.DropAsync("ServiceProvider");
                await docDB.DropAsync("Account");
            }

            Dictionary<string, string> configValues = new Dictionary<string, string>();
            configValues.Add("ConnectionString", connectionString);
            ConfigurationBuilder configuration = new ConfigurationBuilder();
            configuration.AddInMemoryCollection(configValues);
            DirectoryController controller = new DirectoryController(configuration.Build());

            ITEDirectoryServiceProvider serviceProvider = new TEDirectoryServiceProvider();
            serviceProvider.DID = DIDFactory.GenerateNew();

            using (ITEDirectoryDB dirDB = DirectoryServiceUtil.GetDB(connectionString))
            {
                await dirDB.SaveServiceProviderAsync(serviceProvider);
            }

            ITEDirectoryNewAccount newAccount = new TEDirectoryNewAccount();
            newAccount.DID = DIDFactory.GenerateNew();
            newAccount.PublicDID = newAccount.DID.ToPublicDID();
            newAccount.DigitalLinkURL = "www.google.com";
            newAccount.PGLN = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.0.0");
            newAccount.ServiceProviderDID = serviceProvider.DID;
            newAccount.ServiceProviderPGLN = serviceProvider.PGLN;
            newAccount.Sign();

            IActionResult result = await controller.RegisterAccount(newAccount);
            Assert.IsTrue((result is AcceptedResult));

            ITEDirectoryAccount account = await controller.GetAccount(newAccount.PGLN?.ToString());
            Assert.AreEqual(account?.PGLN, newAccount.PGLN);
        }

        [TestMethod]
        public async Task Client()
        {
            // add the service provider to the directory service
            string connectionString = "mongodb://localhost";
            ITEDirectoryServiceProvider serviceProvider = new TEDirectoryServiceProvider();
            serviceProvider.DID = DIDFactory.GenerateNew();
            using (ITEDirectoryDB dirDB = DirectoryServiceUtil.GetDB(connectionString))
            {
                await dirDB.SaveServiceProviderAsync(serviceProvider);
            }

            // initialize the client
            string url = "http://localhost:44392/";
            DirectoryService.Program.Start(url, connectionString);
            ITEDirectoryClient client = TEClientFactory.DirectoryClient(serviceProvider.DID, url);

            // create the account and verify the account was created correctly
            ITEDirectoryNewAccount newAccount = new TEDirectoryNewAccount();
            newAccount.DID = DIDFactory.GenerateNew();
            newAccount.PublicDID = newAccount.DID.ToPublicDID();
            newAccount.DigitalLinkURL = "www.google.com";
            newAccount.PGLN = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.0.0");
            newAccount.ServiceProviderPGLN = serviceProvider.PGLN;
            newAccount.ServiceProviderDID = serviceProvider.DID;
            newAccount.Sign();

            await client.RegisterAccountAsync(newAccount);

            ITEDirectoryAccount account = await client.GetAccountAsync(newAccount.PGLN);
            Assert.AreEqual(account?.PGLN, newAccount.PGLN);
        }
    }
}
