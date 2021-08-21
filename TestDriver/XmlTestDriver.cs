using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.Models;
using TraceabilityEngine.Models.Events;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Models.Locations;
using TraceabilityEngine.Models.Products;
using TraceabilityEngine.Models.TradingParty;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.StaticData;

namespace TestDriver
{
    public class XmlTestDriver : ITETraceabilityMapper
    {
        public List<ITEEvent> WriteEPCISData(string localEvents)
        {
            try
            {
                List<ITEEvent> events = new List<ITEEvent>();
                TEXML xml = TEXML.CreateFromString(localEvents);
                foreach (TEXML xEvent in xml.ChildElements)
                {
                    ITEEvent cte = null;

                    switch (xEvent.Name)
                    {
                        case "FarmHarvestEvent":
                            {
                                cte = new TETransformationEvent();
                                cte.Action = TEEventAction.ADD;
                                cte.BusinessStep = "urn:gdst:bizStep:farmHarvest";

                                ITETransformationEvent tEvent = cte as ITETransformationEvent;
                                foreach (TEXML xProduct in xEvent.Elements("Outputs/Product"))
                                {
                                    IEPC epc = IdentifierFactory.ParseEPC(xProduct["EPC"].Value, out string epcError);
                                    if (string.IsNullOrWhiteSpace(epcError))
                                    {
                                        double weight = xProduct["NetWeight/Value"].GetValueDouble() ?? 0;
                                        string uom = xProduct["NetWeight/UoM"].Value;
                                        tEvent.AddOutput(epc, weight, uom);
                                    }
                                }

                                foreach (TEXML xProduct in xEvent.Elements("Inputs/Product"))
                                {
                                    IEPC epc = IdentifierFactory.ParseEPC(xProduct["EPC"].Value, out string epcError);
                                    if (string.IsNullOrWhiteSpace(epcError))
                                    {
                                        double weight = xProduct["NetWeight/Value"].GetValueDouble() ?? 0;
                                        string uom = xProduct["NetWeight/UoM"].Value;
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
                                foreach (TEXML xProduct in xEvent.Elements("Products/Product"))
                                {
                                    IEPC epc = IdentifierFactory.ParseEPC(xProduct["EPC"].Value, out string epcError);
                                    if (string.IsNullOrWhiteSpace(epcError))
                                    {
                                        double weight = xProduct["NetWeight/Value"].GetValueDouble() ?? 0;
                                        string uom = xProduct["NetWeight/UoM"].Value;
                                        oEvent.AddProduct(epc, weight, uom);
                                    }
                                }

                                break;
                            }
                    }

                    cte.EventID = xEvent["Guid"].Value;
                    DateTimeOffset.TryParse(xEvent["EventTime"].Value, out DateTimeOffset eventTime);
                    cte.EventTime = eventTime;
                    cte.EventTimeOffset = xEvent["EventTimeOffset"].GetValueDouble() ?? 0;

                    IGLN gln = IdentifierFactory.ParseGLN(xEvent["Location/GLN"].Value, out string error);
                    if(gln != null)
                    {
                        cte.Location = new TEEventLocation(gln);
                    }

                    string dataOwnerStr = xEvent["DataOwner/PGLN"].Value;
                    IPGLN dataOwner = IdentifierFactory.ParsePGLN(dataOwnerStr, out error);
                    cte.DataOwner = dataOwner;

                    string ownerStr = xEvent["Owner/PGLN"].Value;
                    IPGLN owner = IdentifierFactory.ParsePGLN(dataOwnerStr, out error);
                    cte.Owner = owner;

                    events.Add(cte);
                }

                return events;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public string MapToLocalEvents(List<ITEEvent> gs1Events)
        {
            try
            {
                TEXML xml = new TEXML("Events");
                foreach (ITEEvent cte in gs1Events)
                {
                    TEXML xEvent = null;

                    if (cte is ITEObjectEvent && cte.Action == TEEventAction.ADD && cte.BusinessStep == "urn:gdst:bizStep:fishingEvent")
                    {
                        ITEObjectEvent oEvent = cte as ITEObjectEvent;
                        xEvent = xml.AddChild("FishingEvent");
                    }
                    else if (cte is ITETransformationEvent && cte.Action == TEEventAction.ADD && cte.BusinessStep == "urn:gdst:bizStep:farmHarvest")
                    {
                        ITETransformationEvent tEvent = cte as ITETransformationEvent;
                        xEvent = xml.AddChild("FarmHarvestEvent");
                    }
                    else
                    {
                        continue;
                    }

                    xEvent.AddChild("Guid", cte.EventID);
                    xEvent.AddChild("EventTime", cte.EventTime.ToString());
                    xEvent.AddChild("EventTimeOffset", cte.EventTimeOffset.ToString());

                    if (cte.DataOwner != null)
                    {
                        TEXML xTP = xEvent.AddChild("DataOwner");
                        xTP.AddChild("PGLN", cte.DataOwner.ToString());
                    }

                    if (cte.Owner != null)
                    {
                        TEXML xTP = xEvent.AddChild("Owner");
                        xTP.AddChild("PGLN", cte.Owner.ToString());
                    }

                    if (cte.Location?.GLN != null)
                    {
                        TEXML xTP = xEvent.AddChild("Location");
                        xTP.AddChild("GLN", cte.Location.GLN.ToString());
                    }

                    if (cte is ITETransformationEvent)
                    {
                        ITETransformationEvent tEvent = cte as ITETransformationEvent;
                        TEXML xInputs = xEvent.AddChild("Inputs");
                        foreach (ITEEventProduct input in tEvent.Inputs)
                        {
                            TEXML xInput = xInputs.AddChild("Product");
                            xInput.AddChild("EPC", input.EPC?.ToString());

                            TEXML xNetWeight = xInput.AddChild("NetWeight");
                            xNetWeight.AddChild("Value", input.Quantity.Value.ToString());
                            xNetWeight.AddChild("UoM", input.Quantity.UoM.UNCode);
                        }

                        TEXML xOutputs = xEvent.AddChild("Outputs");
                        foreach (ITEEventProduct output in tEvent.Outputs)
                        {
                            TEXML xOutput = xOutputs.AddChild("Product");
                                xOutput.AddChild("EPC", output.EPC?.ToString());

                            TEXML xNetWeight = xOutput.AddChild("NetWeight");
                            xNetWeight.AddChild("Value", output.Quantity.Value.ToString());
                            xNetWeight.AddChild("UoM", output.Quantity.UoM.UNCode);
                        }
                    }

                    if (cte is ITEObjectEvent)
                    {
                        ITEObjectEvent oEvent = cte as ITEObjectEvent;
                        TEXML xProducts = xEvent.AddChild("Products");
                        foreach (ITEEventProduct product in oEvent.Products)
                        {
                            TEXML xProduct = xProducts.AddChild("Product");
                            xProduct.AddChild("EPC", product.EPC?.ToString());

                            TEXML xNetWeight = xProduct.AddChild("NetWeight");
                            xNetWeight.AddChild("Value", product.Quantity.Value.ToString());
                            xNetWeight.AddChild("UoM", product.Quantity.UoM.UNCode);
                        }
                    }

                    xml.AddChild(xEvent);
                }
                return xml.XmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public List<ITELocation> MapToGS1Locations(string localLocations)
        {
            try
            {
                List<ITELocation> locations = new List<ITELocation>();

                TEXML xml = TEXML.CreateFromString(localLocations);
                foreach (TEXML xLocation in xml.ChildElements)
                {
                    TELocation location = new TELocation();

                    location.Name = xLocation["Name"].Value;
                    location.GLN = IdentifierFactory.ParseGLN(xLocation["GLN"].Value);
                    location.Description = xLocation["Description"].Value;

                    if (!xLocation["Address"].IsNull)
                    {
                        TEXML xAddress = xLocation["Address"];
                        location.Address = new TEAddress();
                        location.Address.Address1 = xAddress["Address1"].Value;
                        location.Address.Address2 = xAddress["Address2"].Value;
                        location.Address.City = xAddress["City"].Value;
                        location.Address.State = xAddress["State"].Value;
                        location.Address.ZipCode = xAddress["ZipCode"].Value;
                        location.Address.County = xAddress["County"].Value;

                        int? countryISO = xAddress["Country"].GetValueInt32();
                        if (countryISO != null && countryISO > 0)
                        {
                            location.Address.Country = Countries.FromCountryIso(countryISO.Value);
                        }
                    }

                    locations.Add(location);
                }

                return locations;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public string MapToLocalLocations(List<ITELocation> locations)
        {
            try
            {
                TEXML xml = new TEXML("Locations");

                foreach (ITELocation location in locations)
                {
                    TEXML xLocation = xml.AddChild("Location");
                    xLocation.AddChild("Name", location.Name);
                    xLocation.AddChild("GLN", location.GLN?.ToString());
                    xLocation.AddChild("Description", location.Description);
                    if (location.Address != null)
                    {
                        TEXML xAddress = xLocation.AddChild("Address");
                        xAddress.AddChild("Address1", location.Address.Address1);
                        xAddress.AddChild("Address2", location.Address.Address2);
                        xAddress.AddChild("City", location.Address.City);
                        xAddress.AddChild("County", location.Address.County);
                        xAddress.AddChild("State", location.Address.State);
                        xAddress.AddChild("ZipCode", location.Address.ZipCode);
                        if (location.Address.Country != null)
                        {
                            xAddress.AddChild("Country", location.Address.Country.ISO);
                        }
                    }
                }

                return xml.XmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public string MapToLocalTradeItems(List<ITEProduct> products)
        {
            TEXML xml = new TEXML("Products");
            foreach (ITEProduct product in products)
            {
                TEXML xProduct = new TEXML("Product");
                xProduct.AddChild("Name", product.Name);
                xProduct.AddChild("GTIN", product.GTIN);
                xProduct.AddChild("Description", product.Description);
                xml.AddChild(xProduct);
            }
            return xml.PrintXmlString;
        }

        public List<ITEProduct> MapToGS1TradeItems(string localTradeItems)
        {
            List<ITEProduct> products = new List<ITEProduct>();
            TEXML xml = TEXML.CreateFromString(localTradeItems);
            foreach (TEXML xProduct in xml.ChildElements)
            {
                ITEProduct product = new TEProduct();
                product.Name = xProduct["Name"].Value;
                product.GTIN = IdentifierFactory.ParseGTIN(xProduct["GTIN"].Value, out string error);
                product.Description = xProduct["Description"].Value;
                products.Add(product);
            }
            return products;
        }

        public string MapToLocalTradingPartners(List<ITETradingParty> tradingParties)
        {
            TEXML xml = new TEXML("TradingParties");
            foreach (ITETradingParty tp in tradingParties)
            {
                TEXML xProduct = new TEXML("TradingParty");
                xProduct.AddChild("Name", tp.Name);
                xProduct.AddChild("PGLN", tp.PGLN);
                xml.AddChild(xProduct);
            }
            return xml.PrintXmlString;
        }

        public List<ITETradingParty> MapToGS1TradingPartners(string localTradingPartners)
        {
            List<ITETradingParty> tradingParties = new List<ITETradingParty>();
            TEXML xml = TEXML.CreateFromString(localTradingPartners);
            foreach (TEXML xTP in xml.ChildElements)
            {
                ITETradingParty product = new TETradingParty();
                product.Name = xTP["Name"].Value;
                product.PGLN = IdentifierFactory.ParsePGLN(xTP["PGLN"].Value, out string error);
                tradingParties.Add(product);
            }
            return tradingParties;
        }
    }
}
