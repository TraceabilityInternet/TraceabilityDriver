using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.Mappers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Models.Locations;
using TraceabilityEngine.Models;
using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.StaticData;
using System.Linq;
using TraceabilityEngine.Models.TradingParty;
using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Models.Products;

namespace UnitTests.Mappers
{
    [TestClass]
    public class GS1WebVocabTests
    {

        [TestMethod]
        public void Products()
        {
            // we need to make sure that the TEModels assembly loads for the reflective instantiating of interface classes
            ITEProduct p = new TEProduct();

            List<ITEProduct> products = ModelHelp.BuildData<ITEProduct>(1);
            foreach(ITEProduct prod in products)
            {
                ITEProductMapper mapper = new ProductWebVocabMapper();
                string json = mapper.ConvertFromProduct(prod);
                ITEProduct afterProduct = mapper.ConvertToProduct(json);

                string beforeJSON = JsonConvert.SerializeObject(prod, Formatting.Indented);
                string afterJSON = JsonConvert.SerializeObject(afterProduct, Formatting.Indented);

                Assert.AreEqual(beforeJSON, afterJSON);
            }
        }

        [TestMethod]
        public void Locations()
        {
            // we need to make sure that the TEModels assembly loads for the reflective instantiating of interface classes
            ITELocation loc = new TELocation();

            List<ITELocation> locations = ModelHelp.BuildData<ITELocation>(1);
            foreach (ITELocation location in locations)
            {
                location.IsPublic = false;
                location.Archived = false;
                location.ObjectID = null;

                ITELocationMapper mapper = new LocationWebVocabMapper();
                string json = mapper.ConvertFromLocation(location);
                ITELocation afterLocation = mapper.ConvertToLocation(json);

                string beforeJSON = JsonConvert.SerializeObject(location, Formatting.Indented);
                string afterJSON = JsonConvert.SerializeObject(afterLocation, Formatting.Indented);

                Assert.AreEqual(beforeJSON, afterJSON);
            }
        }

        [TestMethod]
        public void TradingParty()
        {
            // we need to make sure that the TEModels assembly loads for the reflective instantiating of interface classes
            ITETradingParty tp = new TETradingParty();

            List<ITETradingParty> tradingParties = ModelHelp.BuildData<ITETradingParty>(1);
            foreach (ITETradingParty tradingParty in tradingParties)
            {
                ITETradingPartyMapper mapper = new TradingPartyWebVocabMapper();
                string json = mapper.ConvertFromTradingParty(tradingParty);
                ITETradingParty afterTP = mapper.ConvertToTradingParty(json);

                string beforeJSON = JsonConvert.SerializeObject(tradingParty, Formatting.Indented);
                string afterJSON = JsonConvert.SerializeObject(afterTP, Formatting.Indented);

                Assert.AreEqual(beforeJSON, afterJSON);
            }
        }
    }
}
