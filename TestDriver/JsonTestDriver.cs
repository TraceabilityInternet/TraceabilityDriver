using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.Models.Events;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Models.Locations;
using TraceabilityEngine.Models.Products;
using TraceabilityEngine.Models.TradingParty;
using TraceabilityEngine.Util;

namespace TestDriver
{
    public class JsonTestDriver : ITETraceabilityMapper
    {
        public List<ITEEvent> MapToGS1Events(string localEvents)
        {
            List<ITEEvent> events = new List<ITEEvent>();
            try
            {
                JArray jArray = JArray.Parse(localEvents);
                foreach (JObject jEvent in jArray)
                {
                    ITEEvent cte = null;

                    string eventType = jEvent.Value<string>("EventType");
                    switch (eventType)
                    {
                        case "FarmHarvestEvent":
                            {
                                cte = new TETransformationEvent();
                                cte.Action = TEEventAction.ADD;
                                cte.BusinessStep = "urn:gdst:bizStep:farmHarvest";

                                ITETransformationEvent tEvent = cte as ITETransformationEvent;
                                foreach (JObject jProduct in jEvent["Outputs"])
                                {
                                    IEPC epc = IdentifierFactory.ParseEPC(jProduct.Value<string>("EPC"), out string epcError);
                                    if (string.IsNullOrWhiteSpace(epcError))
                                    {
                                        double weight = jProduct["NetWeight"]?.Value<double>("Value") ?? 0;
                                        string uom = jProduct["NetWeight"]?.Value<string>("UoM");
                                        tEvent.AddOutput(epc, weight, uom);
                                    }
                                }

                                foreach (JObject jProduct in jEvent["Inputs"])
                                {
                                    IEPC epc = IdentifierFactory.ParseEPC(jProduct.Value<string>("EPC"), out string epcError);
                                    if (string.IsNullOrWhiteSpace(epcError))
                                    {
                                        double weight = jProduct["NetWeight"]?.Value<double>("Value") ?? 0;
                                        string uom = jProduct["NetWeight"]?.Value<string>("UoM");
                                        tEvent.AddInput(epc, weight, uom);
                                    }
                                }

                                break;
                            }
                        case "FishingEvent":
                            {
                                cte = new TEObjectEvent();
                                cte.Action = TEEventAction.ADD;
                                cte.BusinessStep = "urn:gdst:bizStep:fishingEvent";

                                ITEObjectEvent oEvent = cte as ITEObjectEvent;
                                foreach (JObject jProduct in jEvent["Products"])
                                {
                                    IEPC epc = IdentifierFactory.ParseEPC(jProduct.Value<string>("EPC"), out string epcError);
                                    if (string.IsNullOrWhiteSpace(epcError))
                                    {
                                        double weight = jProduct["NetWeight"]?.Value<double>("Value") ?? 0;
                                        string uom = jProduct["NetWeight"]?.Value<string>("UoM");
                                        oEvent.AddProduct(epc, weight, uom);
                                    }
                                }

                                break;
                            }
                    }

                    cte.EventID = jEvent.Value<string>("Guid");
                    DateTimeOffset.TryParse(jEvent.Value<string>("EventTime"), out DateTimeOffset eventTime);
                    cte.EventTime = eventTime;
                    cte.EventTimeOffset = jEvent.Value<double>("EventTimeOffset");

                    IGLN gln = IdentifierFactory.ParseGLN(jEvent.Value<string>("Location"), out string error);
                    if (gln != null)
                    {
                        cte.Location = new TEEventLocation(gln);
                    }

                    string dataOwnerStr = jEvent.Value<string>("DataOwner");
                    IPGLN dataOwner = IdentifierFactory.ParsePGLN(dataOwnerStr, out error);
                    cte.DataOwner = dataOwner;

                    string ownerStr = jEvent.Value<string>("Owner");
                    IPGLN owner = IdentifierFactory.ParsePGLN(dataOwnerStr, out error);
                    cte.Owner = owner;

                    events.Add(cte);
                }

                return events;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
            }
            return events;
        }

        public List<ITELocation> MapToGS1Locations(string localLocations)
        {
            List<ITELocation> locations = new List<ITELocation>();
            try
            {
                JArray jArray = JArray.Parse(localLocations);
                foreach (var jObj in jArray)
                {
                    ITELocation loc = new TELocation();
                    loc.Name = jObj.Value<string>("Name");
                    loc.GLN = IdentifierFactory.ParseGLN(jObj.Value<string>("GLN"), out string error);
                    loc.Description = jObj.Value<string>("Description");
                    locations.Add(loc);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
            }
            return locations;
        }

        public List<ITEProduct> MapToGS1TradeItems(string localTradeItems)
        {
            List<ITEProduct> products = new List<ITEProduct>();
            try
            {
                JArray jArray = JArray.Parse(localTradeItems);
                foreach (var jObj in jArray)
                {
                    ITEProduct product = new TEProduct();
                    product.Name = jObj.Value<string>("Name");
                    product.GTIN = IdentifierFactory.ParseGTIN(jObj.Value<string>("GTIN"), out string error);
                    product.Description = jObj.Value<string>("Description");
                    products.Add(product);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
            }
            return products;
        }

        public List<ITETradingParty> MapToGS1TradingPartners(string localTradingPartners)
        {
            List<ITETradingParty> tps = new List<ITETradingParty>();
            try
            {
                JArray jArray = JArray.Parse(localTradingPartners);
                foreach (var jObj in jArray)
                {
                    ITETradingParty tp = new TETradingParty();
                    tp.Name = jObj.Value<string>("Name");
                    tp.PGLN = IdentifierFactory.ParsePGLN(jObj.Value<string>("PGLN"), out string error);
                    tps.Add(tp);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
            }
            return tps;
        }

        public string MapToLocalEvents(List<ITEEvent> gs1Events)
        {
            JArray jEvents = new JArray();
            try
            {
                foreach (ITEEvent cte in gs1Events)
                {
                    JObject jEvent = new JObject();

                    if (cte is ITEObjectEvent && cte.Action == TEEventAction.ADD && cte.BusinessStep == "urn:gdst:bizStep:fishingEvent")
                    {
                        ITEObjectEvent oEvent = cte as ITEObjectEvent;
                        jEvent["EventType"] = "FishingEvent";
                    }
                    else if (cte is ITETransformationEvent && cte.Action == TEEventAction.ADD && cte.BusinessStep == "urn:gdst:bizStep:farmHarvest")
                    {
                        ITETransformationEvent tEvent = cte as ITETransformationEvent;
                        jEvent["EventType"] = "FarmHarvestEvent";
                    }
                    else
                    {
                        continue;
                    }

                    jEvent["Guid"] = cte.EventID;
                    jEvent["EventTime"] = cte.EventTime.ToString();
                    jEvent["EventTimeOffset"] = cte.EventTimeOffset.ToString();

                    if (cte.DataOwner != null)
                    {
                        jEvent["DataOwner"] = cte.DataOwner?.ToString();
                    }

                    if (cte.Owner != null)
                    {
                        jEvent["Owner"] = cte.Owner?.ToString();
                    }

                    if (cte.Location?.GLN != null)
                    {
                        jEvent["Location"] = cte.Location?.GLN?.ToString();
                    }

                    if (cte is ITETransformationEvent)
                    {
                        ITETransformationEvent tEvent = cte as ITETransformationEvent;
                        JArray jInputs = new JArray();
                        foreach (ITEEventProduct input in tEvent.Inputs)
                        {
                            JObject jInput = new JObject();
                            jInput["EPC"] = input.EPC?.ToString();
                            JObject jNetWeight = new JObject();
                            jNetWeight["Value"] = input.Quantity.Value.ToString();
                            jNetWeight["UoM"] = input.Quantity.UoM.UNCode;
                            jInput["NetWeight"] = jNetWeight;
                            jInputs.Add(jInput);
                        }
                        jEvent["Inputs"] = jInputs;

                        JArray jOutputs = new JArray();
                        foreach (ITEEventProduct output in tEvent.Outputs)
                        {
                            JObject jOutput = new JObject();
                            jOutput["EPC"] = output.EPC?.ToString();
                            JObject jNetWeight = new JObject();
                            jNetWeight["Value"] = output.Quantity.Value.ToString();
                            jNetWeight["UoM"] = output.Quantity.UoM.UNCode;
                            jOutput["NetWeight"] = jNetWeight;
                            jOutputs.Add(jOutput);
                        }
                        jEvent["Outputs"] = jInputs;
                    }

                    if (cte is ITEObjectEvent)
                    {
                        ITEObjectEvent oEvent = cte as ITEObjectEvent;
                        JArray jProducts = new JArray();
                        foreach (ITEEventProduct product in oEvent.Products)
                        {
                            JObject jProduct = new JObject();
                            jProduct["EPC"] = product.EPC?.ToString();
                            JObject jNetWeight = new JObject();
                            jNetWeight["Value"] = product.Quantity.Value.ToString();
                            jNetWeight["UoM"] = product.Quantity.UoM.UNCode;
                            jProduct["NetWeight"] = jNetWeight;
                            jProducts.Add(jProduct);
                        }
                        jEvent["Products"] = jProducts;
                    }

                    jEvents.Add(jEvent);
                }
                return jEvents.ToString();
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
            }
            return jEvents.ToString();
        }

        public string MapToLocalLocations(List<ITELocation> gs1Locations)
        {
            JArray jLocations = new JArray();
            try
            {
                foreach (var location in gs1Locations)
                {
                    JObject jLoc = new JObject();
                    jLoc["Name"] = location.Name;
                    jLoc["GLN"] = location.GLN?.ToString();
                    jLoc["Description"] = location.Description;
                    jLocations.Add(jLoc);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
            }
            return jLocations.ToString();
        }

        public string MapToLocalTradeItems(List<ITEProduct> products)
        {
            JArray jTradeItems = new JArray();
            try
            {
                foreach (var ti in products)
                {
                    JObject jTI = new JObject();
                    jTI["Name"] = ti.Name;
                    jTI["GTIN"] = ti.GTIN?.ToString();
                    jTI["Description"] = ti.Description;
                    jTradeItems.Add(jTI);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
            }
            return jTradeItems.ToString();
        }

        public string MapToLocalTradingPartners(List<ITETradingParty> tradingParties)
        {
            JArray jTPs = new JArray();
            try
            {
                foreach (var tp in tradingParties)
                {
                    JObject jTP = new JObject();
                    jTP["Name"] = tp.Name;
                    jTP["PGLN"] = tp.PGLN?.ToString();
                    jTPs.Add(jTP);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
            }
            return jTPs.ToString();
        }
    }
}
