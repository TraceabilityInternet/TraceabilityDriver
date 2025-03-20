using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Util;

namespace TestSolutionProvider.Controllers
{
    [ApiController]
    [Route("xml")]
    public class XmlController
    {
        public static TEXML xEvents = null;
        public static TEXML xProducts = null;
        public static TEXML xLocations = null;
        public static TEXML xTradingParties = null;

        public XmlController()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            if (xEvents == null) xEvents = loader.ReadXML("TestSolutionProvider.Data.xml.Events.xml");
            if (xProducts == null) xProducts = loader.ReadXML("TestSolutionProvider.Data.xml.TradeItems.xml");
            if (xLocations == null) xLocations = loader.ReadXML("TestSolutionProvider.Data.xml.Locations.xml");
            if (xTradingParties == null) xTradingParties = loader.ReadXML("TestSolutionProvider.Data.xml.TradingParties.xml");
        }

        [HttpGet]
        [Route("{account_id}/{tradingpartner_id}/tradeitem/{gtinStr}")] //removed {tradingpartner_id}
        public string GetTradeItem(string gtinStr, long account_id, long tradingpartner_id)
        {
            try
            {
                TEXML xml = new TEXML("Products");
                foreach (TEXML xTI in xProducts.Elements($"Product[GTIN='{gtinStr}']"))
                {
                    xml.AddChild(xTI);
                }
                return xml.PrintXmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("tradeitems")] //removed {tradingpartner_id}
        public string GetTradeItems()
        {
            try
            {
                return xProducts.XmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("{account_id}/{tradingpartner_id}/location/{glnStr}")]
        public string GetLocation(string glnStr, long account_id, long tradingpartner_id)
        {
            try
            {
                TEXML xml = new TEXML("Locations");
                foreach (TEXML xTI in xLocations.Elements($"Location[GLN='{glnStr}']"))
                {
                    xml.AddChild(xTI);
                }
                return xml.PrintXmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("locations")]
        public string GetLocations()
        {
            try
            {
                return xLocations.XmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("{account_id}/{tradingpartner_id}/tradingpartner/{pglnStr}")]
        public string GetTradingPartner(string pglnStr, long account_id, long tradingpartner_id)
        {
            try
            {
                TEXML xml = new TEXML("TradingParties");
                foreach (TEXML xTI in xTradingParties.Elements($"TradingParty[PGLN='{pglnStr}']"))
                {
                    xml.AddChild(xTI);
                }
                return xml.PrintXmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("tradingparties")]
        public string GetTradingPartners()
        {
            try
            {
                return xTradingParties.XmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("{account_id}/{tradingpartner_id}/events/{epcStr}")]
        public string GetEvents(string epcStr, long account_id, long tradingpartner_id)
        {
            try
            {
                TEXML xml = new TEXML("Events");
                foreach (TEXML xEvent in xEvents.Elements($"//*[Products/Product/EPC='{epcStr}']"))
                {
                    xml.AddChild(xEvent.DeepClone());
                }
                foreach (TEXML xEvent in xEvents.Elements($"//*[Inputs/Product/EPC='{epcStr}']"))
                {
                    xml.AddChild(xEvent.DeepClone());
                }
                foreach (TEXML xEvent in xEvents.Elements($"//*[Outputs/Product/EPC='{epcStr}']"))
                {
                    xml.AddChild(xEvent.DeepClone());
                }
                return xml.PrintXmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("events")]
        public string GetAllEvents()
        {
            try
            {
                return xEvents.XmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("raw/{identifier}")]
        public string GetRaw(string identifier)
        {
            string value = "";
            foreach (TEXML x in xProducts.Elements($"Product[GTIN='{identifier}']"))
            {
                value = x.PrintXmlString;
                return value;
            }
            foreach (TEXML x in xLocations.Elements($"Location[GLN='{identifier}']"))
            {
                value = x.PrintXmlString;
                return value;
            }
            foreach (TEXML x in xTradingParties.Elements($"TradingParty[PGLN='{identifier}']"))
            {
                value = x.PrintXmlString;
                return value;
            }
            foreach (TEXML x in xEvents.Elements($"*[Guid='{identifier}']"))
            {
                value = x.PrintXmlString;
                return value;
            }
            return value;
        }

        [HttpPost]
        [Route("save/{type}")]
        public void SaveData(string type, [FromBody] string dataStr)
        {
            throw new NotImplementedException();
        }
    }
}
