using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Util;

namespace TestSolutionProviderService.Controllers
{
    [ApiController]
    [Route("xml")]
    public class XmlController
    {
        public static TEXML xEvents = new TEXML();
        public static TEXML xProducts = new TEXML();
        public static TEXML xLocations = new TEXML();
        public static TEXML xTradingParties = new TEXML();

        public XmlController()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            xEvents = loader.ReadXML("TestSolutionProviderService.Data.xml.Events.xml");
            xProducts = loader.ReadXML("TestSolutionProviderService.Data.xml.TradeItems.xml");
            xLocations = loader.ReadXML("TestSolutionProviderService.Data.xml.Locations.xml");
            xTradingParties = loader.ReadXML("TestSolutionProviderService.Data.xml.TradingParties.xml");
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
        [Route("{account_id}/{tradingpartner_id}/tradingpartner/{pglnStr}")]
        public string GetTradingPartner(string pglnStr, long account_id, long tradingpartner_id)
        {
            try
            {
                TEXML xml = new TEXML("TradingParties");
                foreach (TEXML xTI in xProducts[$"TradingParty[PGLN='{pglnStr}']"])
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
        [Route("{account_id}/{tradingpartner_id}/tradingparty/{pglnStr}")]
        public string GetTradingParty(string pglnStr, long account_id, long tradingpartner_id)
        {
            try
            {
                TEXML xml = new TEXML("TradingParties");
                foreach (TEXML xTI in xProducts[$"TradingParty[PGLN='{pglnStr}']"])
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
    }
}
