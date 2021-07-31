using DirectoryService;
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

namespace TraceabilityDriverDemo
{
    public static class Utility
    {
        public static readonly string TraceabilityDriverDB01 = "TraceabilityDriver01";
        public static readonly string TraceabilityDriverDB02 = "TraceabililyDriver02";
        public static readonly string ConnectionString01 = "mongodb://localhost";

        public static async Task ClearDatabases()
        {
            using (ITEDocumentDB docDB = new TEMongoDatabase(ConnectionString01, "TEDirectory"))
            {
                await docDB.DropAsync("ServiceProvider");
                await docDB.DropAsync("Account");
            }
            using (ITEDocumentDB docDB = new TEMongoDatabase(ConnectionString01, TraceabilityDriverDB01))
            {
                await docDB.DropAsync("TradingPartner");
                await docDB.DropAsync("Account");
                await docDB.DropAsync("DigitalLink");
            }
            using (ITEDocumentDB docDB = new TEMongoDatabase(ConnectionString01, TraceabilityDriverDB02))
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

        public static async Task<ITDConfiguration> GetConfiguration(string url, string directoryURL, string solutionProviderURL, string dbName)
        {
            ITDConfiguration configuration = new TDConfiguration();
            configuration.APIKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = ConnectionString01;
            configuration.DirectoryURL = directoryURL;
            configuration.MapperClassName = "TestDriver.XmlTestDriver";
            configuration.MapperDLLPath = @"C:\GitHub\TraceabilityInternet\TraceabilityDriver\TestDriver\bin\Debug\net5.0\TestDriver.dll";
            configuration.Mapper = DriverUtil.LoadMapper(configuration.MapperDLLPath, configuration.MapperClassName); // Added to prevent null reference exception
            configuration.RequiresTradingPartnerAuthorization = false;
            configuration.DatabaseName = dbName;
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
                configuration.TradingPartyURLTemplate = $"{solutionProviderURL}/xml/{{account_id}}/{{tradingpartner_id}}/tradingpartner/{{pgln}}";
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

        public static ITEDocumentDB GetDocumentDB(string dbName)
        {
            return new TEMongoDatabase(Utility.ConnectionString01, dbName);
        }
    }
}
