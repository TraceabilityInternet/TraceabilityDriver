using DirectoryService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriverService.Services;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Databases.Mongo;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Service.Util;
using TraceabilityEngine.Util.Security;

namespace UnitTests
{
    public static class UnitTests
    {
        public static readonly string ConnectionString01 = "mongodb://localhost";

        public static async Task ClearDatabases()
        {
            using (ITEDocumentDB docDB = new TEMongoDatabase(ConnectionString01, "TEDirectory"))
            {
                await docDB.DropAsync("ServiceProvider");
                await docDB.DropAsync("Account");
            }
            using (ITEDocumentDB docDB = new TEMongoDatabase(ConnectionString01, "TraceabilityDriver"))
            {
                await docDB.DropAsync("TradingPartner");
                await docDB.DropAsync("Account");
                await docDB.DropAsync("DigitalLink");
            }
        }

        public static async Task<ITEDirectoryServiceProvider> CreateServiceProvider()
        {
            ITEDirectoryServiceProvider serviceProvider = new TEDirectoryServiceProvider();
            serviceProvider.DID = DID.GenerateNew();
            using (ITEDirectoryDB dirDB = DirectoryServiceUtil.GetDB(ConnectionString01))
            {
                await dirDB.SaveServiceProviderAsync(serviceProvider);
            }
            return serviceProvider;
        }

        public static async Task<ITDConfiguration> GetConfiguration(string url, string directoryURL, string solutionProviderURL=null)
        {
            ITDConfiguration configuration = new TDConfiguration();
            configuration.APIKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = ConnectionString01;
            configuration.DirectoryURL = directoryURL;
            configuration.MapperClassName = "TestDriver.XmlTestDriver";
            configuration.MapperDLLPath = @"C:\GitHub\TraceabilityInternet\TraceabilityDriver\TestDriver\bin\Debug\net5.0\TestDriver.dll";
            configuration.Mapper = DriverUtil.LoadMapper(configuration.MapperDLLPath, configuration.MapperClassName); // Added to prevent null reference exception
            configuration.RequiresTradingPartnerAuthorization = false;
            configuration.DatabaseName = "TraceabilityDriver";
            if (!string.IsNullOrWhiteSpace(directoryURL))
            {
                ITEDirectoryServiceProvider serviceProvider = await CreateServiceProvider();
                configuration.ServiceProviderDID = serviceProvider.DID;
                configuration.ServiceProviderPGLN = serviceProvider.PGLN;
            }
            configuration.URL = url;
            if (solutionProviderURL != null)
            {
                configuration.TradeItemURLTemplate = $"{solutionProviderURL}/xml/{{account_id}}/{{tradingpartner_id}}/tradeitem/{{gtin}}";
                configuration.LocationURLTemplate = $"{solutionProviderURL}/xml/{{account_id}}/{{tradingpartner_id}}/location/{{gln}}";
                configuration.TradingPartnerURLTemplate = $"{solutionProviderURL}/xml/{{account_id}}/{{tradingpartner_id}}/tradingpartner/{{pgln}}";
                configuration.EventURLTemplate = $"{solutionProviderURL}/xml/{{account_id}}/{{tradingpartner_id}}/events/{{epc}}"; // John Edit
            }
            return configuration;
        }

        public static void StartTraceabilityDriverService(ITDConfiguration configuration)
        {
            TraceabilityDriverService.Program.Start(configuration);
        }

        public static void StartDirectoryService(string url)
        {
            DirectoryService.Program.Start(url, ConnectionString01);
        }

        public static void StartTestSolutionProvider(string url, string dataURL = "xml", string mapperDLLPath = @"C:\GitHub\TraceabilityInternet\TraceabilityDriver\TestDriver\bin\Debug\net5.0\TestDriver.dll", string mapperClassName = "TestDriver.XmlTestDriver")
        {
            TestSolutionProvider.Program.Start(url, mapperDLLPath, mapperClassName, dataURL, 0, 0, null, null);
        }

        public static ITEDocumentDB GetDocumentDB(string dbName)
        {
            return new TEMongoDatabase(UnitTests.ConnectionString01, dbName);
        }
    }
}
