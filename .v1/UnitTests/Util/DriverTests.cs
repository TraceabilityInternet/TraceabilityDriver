using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestDriver;
using TraceabilityDriverService;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Models;
using TraceabilityEngine.Models.Events;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Models.Locations;
using TraceabilityEngine.Models.Products;
using TraceabilityEngine.Models.TradingParty;
using TraceabilityEngine.StaticData;
using TraceabilityEngine.Util.StaticData;

namespace UnitTests.Util
{
    [TestClass]
    public class DriverTests
    {
        public List<ITEEvent> GetEvents()
        {
            List<ITEEvent> events = new List<ITEEvent>();

            IGLN metalMine = new GLN("urn:epc:id:sgln:08600031303.0");
            IGLN processingPlant = new GLN("urn:epc:id:sgln:08600031303.1");

            IEPC metalOre = new EPC("urn:epc:class:lgtin:08600031303.00.METAL");
            IEPC steel = new EPC("urn:epc:class:lgtin:08600031303.01.STEEL");

            IPGLN metalMineOwner = new PGLN("urn:epc:id:sgln:08600031303.4");
            IPGLN processor1 = new PGLN("urn:epc:id:sgln:08600031303.5");

            IEPC sscc = new EPC("urn:sscc:1234567890");

            // create
            {
                ITEObjectEvent createEvent = new TEObjectEvent();
                createEvent.EventTime = DateTime.UtcNow.AddDays(-1);
                createEvent.DataOwner = metalMineOwner;
                createEvent.Owner = metalMineOwner;
                createEvent.Action = TEEventAction.ADD;
                createEvent.BusinessStep = "urn:gdst:bizStep:fishingEvent";
                createEvent.Location = new TEEventLocation(metalMine);
                createEvent.AddProduct(metalOre, 100, "KGM");
                events.Add(createEvent);
            }

            // transformation
            {
                ITETransformationEvent transformEvent = new TETransformationEvent();
                transformEvent.EventTime = DateTime.UtcNow;
                transformEvent.Owner = processor1;
                transformEvent.DataOwner = processor1;
                transformEvent.Action = TEEventAction.ADD;
                transformEvent.BusinessStep = "urn:gdst:bizStep:farmHarvest";
                transformEvent.Location = new TEEventLocation(processingPlant);
                transformEvent.AddInput(metalOre, 100, "KGM");
                transformEvent.AddOutput(steel, 75, "KGM");
                events.Add(transformEvent);
            }

            return events;
        }

        public List<ITELocation> GetLocations()
        {
            List<ITELocation> locations = new List<ITELocation>();

            ITELocation location = new TELocation();
            location.Name = "Processing Plant";
            location.GLN = IdentifierFactory.ParseGLN("urn:epc:id:sgln:08600031303.0");
            location.Description = "This is where things are processed.";
            location.Address = new TEAddress()
            {
                Address1 = "15814 Blanco Trails Ln",
                City = "Cypress",
                State = "TX",
                ZipCode = "77429",
                County = "Harris",
                Country = Countries.FromCountryIso(840)
            };

            locations.Add(location);
            return locations;
        }

        public List<ITEProduct> GetProducts()
        {
            List<ITEProduct> products = new List<ITEProduct>();

            IGTIN gtin = new GTIN("urn:epc:idpat:sgtin:08600031303.00");
            ITEProduct product = new TEProduct();
            product.Name = "Yellowfin Tuna";
            product.GTIN = gtin;
            product.Description = "This is a whole yellow fin tuna.";

            products.Add(product);
            return products;
        }

        public List<ITETradingParty> GetTradingParties()
        {
            List<ITETradingParty> tradingParties = new List<ITETradingParty>();
            IPGLN pgln = new PGLN("urn:epc:id:sgln:08600031303.0");
            ITETradingParty tp = new TETradingParty();
            tp.Name = "Trading Party #1";
            tp.PGLN = pgln;

            tradingParties.Add(tp);
            return tradingParties;
        }

        [TestMethod]
        public void Load()
        {
            string dll = @"C:\GitHub\TraceabilityInternet\TraceabilityDriver\TestDriver\bin\Debug\net5.0\TestDriver.dll";
            string className = "TestDriver.XmlTestDriver";
            ITETraceabilityMapper driver = TraceabilityDriverServiceFactory.LoadMapper(dll, className);
            Assert.IsNotNull(driver);
        }

        [TestMethod]
        public void XmlDriver_Events()
        {
            string dll = @"C:\GitHub\TraceabilityInternet\TraceabilityDriver\TestDriver\bin\Debug\net5.0\TestDriver.dll";
            string className = "TestDriver.XmlTestDriver";
            ITETraceabilityMapper driver = TraceabilityDriverServiceFactory.LoadMapper(dll, className);
            Assert.IsNotNull(driver);

            ITEEventMapper mapper = new EPCISJsonMapper_2_0();

            List<ITEEvent> events = GetEvents();
            string xml = driver.MapToLocalEvents(events);
            List<ITEEvent> eventsAfter = driver.MapToGS1Events(xml);

            string gs1Json = mapper.ConvertFromEvents(events);
            string gs1JsonAfter = mapper.ConvertFromEvents(eventsAfter);

            JObject gs1JObj = JObject.Parse(gs1Json);
            JObject gs1JObjAfter = JObject.Parse(gs1Json);

            gs1JObj["creationDate"] = "";
            gs1JObjAfter["creationDate"] = "";

            gs1Json = gs1JObj.ToString();
            gs1JsonAfter = gs1JObjAfter.ToString();

            if (Debugger.IsAttached && gs1Json != gs1JsonAfter)
            {
                Debugger.Break();
            }
            Assert.AreEqual(gs1Json, gs1JsonAfter);
        }

        [TestMethod]
        public void XmlDriver_Locations()
        {
            string dll = @"C:\GitHub\TraceabilityInternet\TraceabilityDriver\TestDriver\bin\Debug\net5.0\TestDriver.dll";
            string className = "TestDriver.XmlTestDriver";
            ITETraceabilityMapper driver = TraceabilityDriverServiceFactory.LoadMapper(dll, className);
            Assert.IsNotNull(driver);

            ITELocationMapper mapper = new LocationWebVocabMapper();

            List<ITELocation> allLocations = GetLocations();
            foreach (ITELocation location in allLocations)
            {
                List<ITELocation> locations = new List<ITELocation>() { location };
                string xml = driver.MapToLocalLocations(locations);
                List<ITELocation> locationsAfter = driver.MapToGS1Locations(xml);

                string gs1Json = mapper.ConvertFromLocation(location);
                string gs1JsonAfter = mapper.ConvertFromLocation(locationsAfter.FirstOrDefault());

                JObject gs1JObj = JObject.Parse(gs1Json);
                JObject gs1JObjAfter = JObject.Parse(gs1Json);

                gs1JObj["creationDate"] = "";
                gs1JObjAfter["creationDate"] = "";

                gs1Json = gs1JObj.ToString();
                gs1JsonAfter = gs1JObjAfter.ToString();

                if (Debugger.IsAttached && gs1Json != gs1JsonAfter)
                {
                    Debugger.Break();
                }
                Assert.AreEqual(gs1Json, gs1JsonAfter);
            }
        }

        [TestMethod]
        public void XmlDriver_TradeItems()
        {
            string dll = @"C:\GitHub\TraceabilityInternet\TraceabilityDriver\TestDriver\bin\Debug\net5.0\TestDriver.dll";
            string className = "TestDriver.XmlTestDriver";
            ITETraceabilityMapper driver = TraceabilityDriverServiceFactory.LoadMapper(dll, className);
            Assert.IsNotNull(driver);

            ITEProductMapper mapper = new ProductWebVocabMapper();

            List<ITEProduct> allProducts = GetProducts();
            foreach (ITEProduct product in allProducts)
            {
                List<ITEProduct> products = new List<ITEProduct>() { product };
                string xml = driver.MapToLocalTradeItems(products);
                List<ITEProduct> productsAfter = driver.MapToGS1TradeItems(xml);

                string gs1Json = mapper.ConvertFromProduct(product);
                string gs1JsonAfter = mapper.ConvertFromProduct(productsAfter.FirstOrDefault());

                JObject gs1JObj = JObject.Parse(gs1Json);
                JObject gs1JObjAfter = JObject.Parse(gs1Json);

                gs1JObj["creationDate"] = "";
                gs1JObjAfter["creationDate"] = "";

                gs1Json = gs1JObj.ToString();
                gs1JsonAfter = gs1JObjAfter.ToString();

                if (Debugger.IsAttached && gs1Json != gs1JsonAfter)
                {
                    Debugger.Break();
                }
                Assert.AreEqual(gs1Json, gs1JsonAfter);
            }
        }

        [TestMethod]
        public void XmlDriver_TradingParty()
        {
            string dll = @"C:\GitHub\TraceabilityInternet\TraceabilityDriver\TestDriver\bin\Debug\net5.0\TestDriver.dll";
            string className = "TestDriver.XmlTestDriver";
            ITETraceabilityMapper driver = TraceabilityDriverServiceFactory.LoadMapper(dll, className);
            Assert.IsNotNull(driver);

            ITETradingPartyMapper mapper = new TradingPartyWebVocabMapper();

            List<ITETradingParty> allTPs = GetTradingParties();
            foreach (ITETradingParty tp in allTPs)
            {
                List<ITETradingParty> tradingParties = new List<ITETradingParty>() { tp };
                string xml = driver.MapToLocalTradingPartners(tradingParties);
                List<ITETradingParty> tpAfter = driver.MapToGS1TradingPartners(xml);

                string gs1Json = mapper.ConvertFromTradingParty(tp);
                string gs1JsonAfter = mapper.ConvertFromTradingParty(tpAfter.FirstOrDefault());

                JObject gs1JObj = JObject.Parse(gs1Json);
                JObject gs1JObjAfter = JObject.Parse(gs1Json);

                gs1JObj["creationDate"] = "";
                gs1JObjAfter["creationDate"] = "";

                gs1Json = gs1JObj.ToString();
                gs1JsonAfter = gs1JObjAfter.ToString();

                if (Debugger.IsAttached && gs1Json != gs1JsonAfter)
                {
                    Debugger.Break();
                }
                Assert.AreEqual(gs1Json, gs1JsonAfter);
            }
        }

        [TestMethod]
        public void JsonDriver_Events()
        {
            ITETraceabilityMapper driver = new JsonTestDriver();
            Assert.IsNotNull(driver);

            ITEEventMapper mapper = new EPCISJsonMapper_2_0();

            List<ITEEvent> events = GetEvents();
            string json = driver.MapToLocalEvents(events);
            List<ITEEvent> eventsAfter = driver.MapToGS1Events(json);

            string gs1Json = mapper.ConvertFromEvents(events);
            string gs1JsonAfter = mapper.ConvertFromEvents(eventsAfter);

            JObject gs1JObj = JObject.Parse(gs1Json);
            JObject gs1JObjAfter = JObject.Parse(gs1Json);

            gs1JObj["creationDate"] = "";
            gs1JObjAfter["creationDate"] = "";

            gs1Json = gs1JObj.ToString();
            gs1JsonAfter = gs1JObjAfter.ToString();

            if (Debugger.IsAttached && gs1Json != gs1JsonAfter)
            {
                Debugger.Break();
            }
            Assert.AreEqual(gs1Json, gs1JsonAfter);
        }

        [TestMethod]
        public void JsonDriver_Locations()
        {
            ITETraceabilityMapper driver = new JsonTestDriver();
            Assert.IsNotNull(driver);

            ITELocationMapper mapper = new LocationWebVocabMapper();

            List<ITELocation> allLocations = GetLocations();
            foreach (ITELocation location in allLocations)
            {
                List<ITELocation> locations = new List<ITELocation>() { location };
                string xml = driver.MapToLocalLocations(locations);
                List<ITELocation> locationsAfter = driver.MapToGS1Locations(xml);

                string gs1Json = mapper.ConvertFromLocation(location);
                string gs1JsonAfter = mapper.ConvertFromLocation(locationsAfter.FirstOrDefault());

                JObject gs1JObj = JObject.Parse(gs1Json);
                JObject gs1JObjAfter = JObject.Parse(gs1Json);

                gs1JObj["creationDate"] = "";
                gs1JObjAfter["creationDate"] = "";

                gs1Json = gs1JObj.ToString();
                gs1JsonAfter = gs1JObjAfter.ToString();

                if (Debugger.IsAttached && gs1Json != gs1JsonAfter)
                {
                    Debugger.Break();
                }
                Assert.AreEqual(gs1Json, gs1JsonAfter);
            }
        }

        [TestMethod]
        public void JsonDriver_TradeItems()
        {
            ITETraceabilityMapper driver = new JsonTestDriver();
            Assert.IsNotNull(driver);

            ITEProductMapper mapper = new ProductWebVocabMapper();

            List<ITEProduct> allProducts = GetProducts();
            foreach (ITEProduct product in allProducts)
            {
                List<ITEProduct> products = new List<ITEProduct>() { product };
                string xml = driver.MapToLocalTradeItems(products);
                List<ITEProduct> productsAfter = driver.MapToGS1TradeItems(xml);

                string gs1Json = mapper.ConvertFromProduct(product);
                string gs1JsonAfter = mapper.ConvertFromProduct(productsAfter.FirstOrDefault());

                JObject gs1JObj = JObject.Parse(gs1Json);
                JObject gs1JObjAfter = JObject.Parse(gs1Json);

                gs1JObj["creationDate"] = "";
                gs1JObjAfter["creationDate"] = "";

                gs1Json = gs1JObj.ToString();
                gs1JsonAfter = gs1JObjAfter.ToString();

                if (Debugger.IsAttached && gs1Json != gs1JsonAfter)
                {
                    Debugger.Break();
                }
                Assert.AreEqual(gs1Json, gs1JsonAfter);
            }
        }

        [TestMethod]
        public void JsonDriver_TradingParty()
        {
            ITETraceabilityMapper driver = new JsonTestDriver();
            Assert.IsNotNull(driver);

            ITETradingPartyMapper mapper = new TradingPartyWebVocabMapper();

            List<ITETradingParty> allTPs = GetTradingParties();
            foreach (ITETradingParty tp in allTPs)
            {
                List<ITETradingParty> tradingParties = new List<ITETradingParty>() { tp };
                string xml = driver.MapToLocalTradingPartners(tradingParties);
                List<ITETradingParty> tpAfter = driver.MapToGS1TradingPartners(xml);

                string gs1Json = mapper.ConvertFromTradingParty(tp);
                string gs1JsonAfter = mapper.ConvertFromTradingParty(tpAfter.FirstOrDefault());

                JObject gs1JObj = JObject.Parse(gs1Json);
                JObject gs1JObjAfter = JObject.Parse(gs1Json);

                gs1JObj["creationDate"] = "";
                gs1JObjAfter["creationDate"] = "";

                gs1Json = gs1JObj.ToString();
                gs1JsonAfter = gs1JObjAfter.ToString();

                if (Debugger.IsAttached && gs1Json != gs1JsonAfter)
                {
                    Debugger.Break();
                }
                Assert.AreEqual(gs1Json, gs1JsonAfter);
            }
        }
    }
}
