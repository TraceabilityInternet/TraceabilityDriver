using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriverService.Controllers;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Mappers;

namespace UnitTests.Services
{
    //[TestClass]
    //public class MasterDataServiceTests
    //{
    //    [TestMethod]
    //    public async Task Controller()
    //    {
    //        string solutionProviderURL = "http://localhost:1340";
    //        string gtin = "urn:epc:idpat:sgtin:08600031303.00";
    //        string connectionString = "mongodb://localhost";
    //        string tradeitemUrlTemplate = solutionProviderURL + "/xml/{account_id}/tradeitem/{gtin}";
    //        string locationUrlTemplate = solutionProviderURL + "/xml/{account_id}/location/{gtin}";
    //        string tradingPartyUrlTemplate = solutionProviderURL + "/xml/{account_id}/tradingpartner/{gtin}";
    //        string dll = @"C:\FOTFS\TraceabilityEngine\TestDriver\bin\Debug\net5.0\TestDriver.dll";
    //        string className = "TestDriver.XmlTestDriver";

    //        // build the configuration
    //        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
    //        Dictionary<string, string> settings = new Dictionary<string, string>();
    //        settings.Add("ConnectionString", connectionString);
    //        settings.Add("TradeItemURLTemplate", tradeitemUrlTemplate);
    //        settings.Add("DriverDLLPath", dll);
    //        settings.Add("DriverClassName", className);
    //        settings.Add("RequiresAuthorization", "false");
    //        configBuilder.AddInMemoryCollection(settings);
    //        IConfiguration config = configBuilder.Build();

    //        // build the query controller
    //        MasterDataController controller = new MasterDataController(config);

    //        // start the Test Solution Provider Service
    //        TestSolutionProviderService.Program.Start(solutionProviderURL);

    //        // get the trade items
    //        IActionResult result = await controller.GetTradeItem(gtin);
    //        Assert.IsTrue((result is OkObjectResult));
    //        string gs1Json = (result as OkObjectResult).Value.ToString();
    //        Assert.IsTrue(!string.IsNullOrWhiteSpace(gs1Json));
    //        ITEProductMapper mapper = new ProductWebVocabMapper();
    //        ITEProduct product = mapper.ConvertToProduct(gs1Json);
    //        Assert.IsNotNull(product);
    //    }
    //}
}
