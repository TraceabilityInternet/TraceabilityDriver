using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util;

namespace TestSolutionProvider.Controllers
{
    [ApiController]
    [Route("json")]
    public class JsonController : ControllerBase
    {
        private static JArray jEvents = new JArray();
        private static JArray jProducts = new JArray();
        private static JArray jLocations = new JArray();
        private static JArray jTradingParties = new JArray();

        public JsonController()
        {
            
        }

        [HttpGet]
        [Route("{account_id}/{tradingpartner_id}/tradeitem/{gtinStr}")] //removed {tradingpartner_id}
        public string GetTradeItem(string gtinStr, long account_id, long tradingpartner_id)
        {
            try
            {
                JArray jProds = new JArray();
                foreach (var jProd in jProducts)
                {
                    if(jProd.Value<string>("GTIN") == gtinStr)
                    {
                        jProds.Add(jProd);
                        break;
                    }
                }
                return jProds.ToString(Newtonsoft.Json.Formatting.Indented);
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
                return jProducts.ToString(Newtonsoft.Json.Formatting.Indented);
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
                JArray jResults = new JArray();
                foreach (var location in jLocations)
                {
                    if (location["GLN"].ToString() == glnStr)
                    {
                        jResults.Add(location);
                        break;
                    }
                }
                return jResults.ToString(Newtonsoft.Json.Formatting.Indented);
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
                return jLocations.ToString(Newtonsoft.Json.Formatting.Indented);
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
                JArray jResults = new JArray();
                foreach (var tp in jTradingParties)
                {
                    if (tp["PGLN"].ToString() == pglnStr)
                    {
                        jResults.Add(tp);
                        break;
                    }
                }
                return jResults.ToString(Newtonsoft.Json.Formatting.Indented);
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
                return jTradingParties.ToString(Newtonsoft.Json.Formatting.Indented);
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
                JArray jResults = new JArray();



                return jResults.ToString(Newtonsoft.Json.Formatting.Indented);
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
                return jEvents.ToString(Newtonsoft.Json.Formatting.Indented);
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
            foreach (var j in jProducts)
            {
                if (j.Value<string>("GTIN") == identifier)
                {
                    value = j.ToString(Newtonsoft.Json.Formatting.Indented);
                    return value;
                }
            }
            foreach (var j in jLocations)
            {
                if (j.Value<string>("GLN") == identifier)
                {
                    value = j.ToString(Newtonsoft.Json.Formatting.Indented);
                    return value;
                }
            }
            foreach (var j in jTradingParties)
            {
                if (j.Value<string>("PGLN") == identifier)
                {
                    value = j.ToString(Newtonsoft.Json.Formatting.Indented);
                    return value;
                }
            }
            foreach (var j in jEvents)
            {
                if (j.Value<string>("Guid") == identifier)
                {
                    value = j.ToString(Newtonsoft.Json.Formatting.Indented);
                    return value;
                }
            }
            return value;
        }

        [HttpPost]
        [Route("save/{type}")]
        public async Task SaveData(string type)
        {
            JObject json = null;
            try
            {
                string dataStr = "";
                using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    dataStr = await reader.ReadToEndAsync();
                }
                switch (type.ToLower())
                {
                    case "event":
                    case "events":
                        {
                            JArray jArr = JArray.Parse(dataStr);
                            foreach (var jObj in jArr)
                            {
                                bool found = false;
                                foreach (var cte in jEvents)
                                {
                                    if (cte.Value<string>("Guid") == jObj.Value<string>("Guid"))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (found == false)
                                {
                                    jEvents.Add(jObj);
                                }
                            }
                            break;
                        }
                    case "tradeitem":
                        {
                            JArray jArr = JArray.Parse(dataStr);
                            foreach (var jObj in jArr)
                            {
                                bool found = false;
                                foreach (var cte in jProducts)
                                {
                                    if (cte.Value<string>("GTIN") == jObj.Value<string>("GTIN"))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (found == false)
                                {
                                    jProducts.Add(jObj);
                                }
                            }
                            break;
                        }
                    case "tradingparty":
                        {
                            JArray jArr = JArray.Parse(dataStr);
                            foreach (var jObj in jArr)
                            {
                                bool found = false;
                                foreach (var cte in jTradingParties)
                                {
                                    if (cte.Value<string>("PGLN") == jObj.Value<string>("PGLN"))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (found == false)
                                {
                                    jTradingParties.Add(jObj);
                                }
                            }
                            break;
                        }
                    case "location":
                        {
                            JArray jArr = JArray.Parse(dataStr);
                            foreach (var jObj in jArr)
                            {
                                bool found = false;
                                foreach (var cte in jLocations)
                                {
                                    if (cte.Value<string>("GLN") == jObj.Value<string>("GLN"))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (found == false)
                                {
                                    jLocations.Add(jObj);
                                }
                            }
                            break;
                        }
                }
            }
            catch(Exception)
            {
                // do nothing
            }
        }
    }
}

