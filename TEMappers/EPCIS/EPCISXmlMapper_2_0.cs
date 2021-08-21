using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Models;
using TraceabilityEngine.Models.Events;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Mappers.EPCIS
{
    public class EPCISXmlMapper_2_0 : ITEEPCISMapper
    {
        private bool ValidateSchema(TEXML xml)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public string ConvertFromEvents(ITETraceabilityData data)
        {
            try
            {
                TEXML xml = new TEXML("epcis:EPCISDocument");

                // add in the header information

                // add in the events
                TEXML xEventList = xml.AddChild("EPCISBody").AddChild("EventList");
                foreach (var e in data.Events)
                {
                    TEXML xEvent = null;

                    if (e is ITEObjectEvent) xEvent = WriteObjectEvent(e as ITEObjectEvent);
                    if (e is ITETransactionEvent) xEvent = WriteTransactionEvent(e as ITETransactionEvent);
                    if (e is ITEAggregationEvent) xEvent = WriteAggregationEvent(e as ITEAggregationEvent);
                    if (e is ITEAssociationEvent) xEvent = WriteAssociationEvent(e as ITEAssociationEvent);

                    if (xEvent != null)
                    {
                        if (e is ITETransformationEvent)
                        {
                            if (xEventList["extension"].IsNull)
                            {
                                xEventList.AddChild("extension");
                            }
                            xEventList["extension"].AddChild(xEvent);
                        }
                        else
                        {
                            xEventList.AddChild(xEvent);
                        }
                    }
                }

                return xml.PrintXmlString;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private TEXML WriteObjectEvent(ITEObjectEvent e)
        {
            throw new NotImplementedException();
        }

        private TEXML WriteTransactionEvent(ITETransactionEvent e)
        {
            throw new NotImplementedException();
        }

        private TEXML WriteTransformationEvent(ITETransformationEvent e)
        {
            throw new NotImplementedException();
        }

        private TEXML WriteAssociationEvent(ITEAssociationEvent e)
        {
            throw new NotImplementedException();
        }

        private TEXML WriteAggregationEvent(ITEAggregationEvent e)
        {
            throw new NotImplementedException();
        }

        private void WriteCommonDataElements(ITEEvent e, TEXML xEvent, TEXML xExtensionBase)
        {
            try
            {

            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        #region Convert from XML to C# Models

        public ITETraceabilityData ReadEPCISData(string value)
        {
            try
            {
                ITETraceabilityData data = new TETraceabilityData();

                // validate the XML against the schema
                TEXML xml = TEXML.CreateFromString(value);
                if (!ValidateSchema(xml))
                {
                    throw new Exception("Failed to validate the XML against the schema.");
                }

                // foreach object event...
                foreach (var xObject in xml.Elements("//EPCISBody/EventList/ObjectEvent"))
                {
                    ITEEvent e = ReadObjectEvent(xObject);
                    data.Events.Add(e);
                }

                // foreach transformation event...
                foreach (var xTransform in xml.Elements("//EPCISBody/EventList/extension/TransformationEvent"))
                {
                    ITEEvent e = ReadTransformationEvent(xTransform);
                    data.Events.Add(e);
                }

                // foreach aggregate event...
                foreach (var xTransform in xml.Elements("//EPCISBody/EventList/AggregationEvent"))
                {
                    ITEEvent e = ReadAggregationEvent(xTransform);
                    data.Events.Add(e);
                }

                // foreach transaction event...
                foreach (var xTransform in xml.Elements("//EPCISBody/EventList/TransactionEvent"))
                {
                    ITEEvent e = ReadTransactionEvent(xTransform);
                    data.Events.Add(e);
                }

                // foreach association event...
                foreach (var xTransform in xml.Elements("//EPCISBody/EventList/AssociationEvent"))
                {
                    ITEEvent e = ReadAssociationEvent(xTransform);
                    data.Events.Add(e);
                }

                return data;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private void ReadCommonDataElements(TEXML xEvent, ITEEvent e)
        {
            try
            {
                // action
                if (!xEvent["action"].IsNull)
                {
                    if(Enum.TryParse<TEEventAction>(xEvent["action"].Value, out TEEventAction action))
                    {
                        e.Action = action;
                    }
                }

                // attachments
                e.Attachments = new List<Interfaces.Models.Common.ITEAttachment>();
                foreach (var xAttachment in xEvent["//te:attachments"])
                {
                    throw new NotImplementedException();
                }

                // business step
                e.BusinessStep = xEvent["bizStep"].Value;

                // disposition
                e.Disposition = xEvent["disposition"].Value;

                // persistent disposition
                if (!xEvent["persistentDisposition"].IsNull)
                {
                    e.PersistentDisposition = new TEPersistentDisposition();
                    foreach (var xSet in xEvent.Elements("persistentDisposition/set"))
                    {
                        e.PersistentDisposition.Set.Add(xSet.Value);
                    }
                    foreach (var xUnset in xEvent.Elements("persistentDisposition/unset"))
                    {
                        e.PersistentDisposition.Unset.Add(xUnset.Value);
                    }
                }

                // business transactions
                foreach (var xBizTransaction in xEvent.Elements("bizTransactionList/bizTransaction"))
                {
                    ITEEventBusinessTransaction bizTransaction = new TEEventBusinessTransaction();
                    bizTransaction.RawType = xBizTransaction.Attribute("type");
                    bizTransaction.Value = xBizTransaction.Value;
                    e.BusinessTransactions.Add(bizTransaction);
                }

                // certificates
                foreach (var xCertificate in xEvent.Elements("cbvmda:certificationList/cbvmda:certification"))
                {
                    throw new NotImplementedException();
                }

                // data owner
                e.DataOwner = TryParsePGLN(xEvent, "cbvmda:informationProvider", "extension/cbvmda:informationProvider");

                // destination list
                foreach (var xDestination in xEvent["//destinationList/destination"])
                {
                    ITEEventDestination dest = new TEEventDestination();
                    dest.RawType = xDestination.Attribute("type");
                    dest.Value = xDestination.Value;
                    e.DestinationList.Add(dest);
                }

                // error declaration
                e.ErrorDeclaration = ReadErrorDeclaration(xEvent);

                // event id
                e.EventID = xEvent["baseExtension/eventID"].Value ?? xEvent["eventID"].Value;

                // event time and event timezone offset
                e.EventTime = DateTime.Parse(xEvent["eventTime"].Value);
                e.EventTimeOffset = TimeSpan.Parse(xEvent["eventTimeZoneOffset"].Value).TotalHours;

                // location
                if (!xEvent["bizLocation"].IsNull)
                {
                    e.Location = new TEEventLocation();
                    e.Location.GLN = TryParseGLN(xEvent, "bizLocation/id");
                }

                // product owner
                e.Owner = TryParsePGLN(xEvent, "gdst:productOwner", "extension/gdst:productOwner");

                // read point
                if (!xEvent["readPoint"].IsNull)
                {
                    e.ReadPoint = new TEEventReadPoint();
                    e.ReadPoint.ID = xEvent["readPoint/id"].Value;
                }

                // source list
                foreach (var xSource in xEvent["//sourceList/source"])
                {
                    ITEEventSource source = new TEEventSource();
                    source.RawType = xSource.Attribute("type");
                    source.Value = xSource.Value;
                    e.SourceList.Add(source);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private ITEEvent ReadObjectEvent(TEXML xObject)
        {
            try
            {
                ITEObjectEvent e = new TEObjectEvent();

                // read the epc list
                foreach (var xEPC in xObject.Elements("/epcList/epc"))
                {
                    IEPC epc = TryParseEPC(xEPC);
                    e.AddProduct(epc);
                }

                // read the quantity list
                foreach (var xQuantity in xObject.Elements("/extension/quantityList/quantityElement"))
                {
                    IEPC epc = TryParseEPC(xQuantity, "epcClass");
                    double quantity = xQuantity["quantity"].GetValueDouble() ?? 0;
                    string uom = xQuantity["uom"].Value;
                    e.AddProduct(epc, quantity, uom);
                }

                ReadCommonDataElements(xObject, e);
                return e;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private ITEEvent ReadTransactionEvent(TEXML xTransaction)
        {
            try
            {
                ITETransactionEvent e = new TETransactionEvent();

                // read the epc list
                foreach (var xEPC in xTransaction.Elements("/epcList/epc"))
                {
                    IEPC epc = TryParseEPC(xEPC);
                    e.AddProduct(epc);
                }

                // read the quantity list
                foreach (var xQuantity in xTransaction.Elements("/extension/quantityList/quantityElement"))
                {
                    IEPC epc = TryParseEPC(xQuantity, "epcClass");
                    double quantity = xQuantity["quantity"].GetValueDouble() ?? 0;
                    string uom = xQuantity["uom"].Value;
                    e.AddProduct(epc, quantity, uom);
                }

                ReadCommonDataElements(xTransaction, e);
                return e;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private ITEEvent ReadTransformationEvent(TEXML xTransform)
        {
            try
            {
                ITETransformationEvent e = new TETransformationEvent();

                // read the input epc list
                foreach (var xEPC in xTransform.Elements("/inputEPCList/epc"))
                {
                    IEPC epc = TryParseEPC(xEPC);
                    e.AddInput(epc);
                }

                // read the output epc list
                foreach (var xEPC in xTransform.Elements("/outputEPCList/epc"))
                {
                    IEPC epc = TryParseEPC(xEPC);
                    e.AddOutput(epc);
                }

                // read the input quantity list
                foreach (var xQuantity in xTransform.Elements("/inputQuantityList/quantityElement"))
                {
                    IEPC epc = TryParseEPC(xQuantity, "epcClass");
                    double quantity = xQuantity["quantity"].GetValueDouble() ?? 0;
                    string uom = xQuantity["uom"].Value;
                    e.AddInput(epc, quantity, uom);
                }

                // read the output quantity list
                foreach (var xQuantity in xTransform.Elements("/outputQuantityList/quantityElement"))
                {
                    IEPC epc = TryParseEPC(xQuantity, "epcClass");
                    double quantity = xQuantity["quantity"].GetValueDouble() ?? 0;
                    string uom = xQuantity["uom"].Value;
                    e.AddOutput(epc, quantity, uom);
                }

                ReadCommonDataElements(xTransform, e);
                return e;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private ITEEvent ReadAggregationEvent(TEXML xAggregate)
        {
            try
            {
                ITEAggregationEvent e = new TEAggregationEvent();

                // read the parent
                e.ParentID = TryParseEPC(xAggregate, "parentID");

                // child epcs
                foreach (var xEPC in xAggregate.Elements("/childEPCs/epc"))
                {
                    IEPC epc = TryParseEPC(xEPC);
                    e.AddChild(epc);
                }

                ReadCommonDataElements(xAggregate, e);
                return e;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private ITEEvent ReadAssociationEvent(TEXML xAssociation)
        {
            try
            {
                ITEAssociationEvent e = new TEAssociationEvent();

                // read the parent
                e.ParentID = TryParseEPC(xAssociation, "parentID");

                // child epcs
                foreach (var xEPC in xAssociation.Elements("/childEPCs/epc"))
                {
                    IEPC epc = TryParseEPC(xEPC);
                    e.AddChild(epc);
                }

                ReadCommonDataElements(xAssociation, e);
                return e;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private ITEErrorDeclaration ReadErrorDeclaration(TEXML xEvent)
        {
            try
            {
                ITEErrorDeclaration error = null;

                TEXML xError = xEvent["errorDeclaration"];
                if (xError.IsNull)
                {
                    xError = xEvent["extension/errorDeclaration"];
                }
                if (!xError.IsNull)
                {
                    error = new TEErrorDeclaration();
                    error.DeclarationTime = DateTime.Parse(xError["declarationTime"].Value);
                    error.RawReason = xError["reason"].Value;
                    foreach (var xCID in xError.Elements("correctiveEventIDs/correctiveEventID"))
                    {
                        error.CorrectingEventIDs.Add(xCID.Value);
                    }
                }

                return error;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private IEPC TryParseEPC(TEXML x, params string[] paths)
        {
            try
            {
                IEPC epc = null;
                string error = null;
                string value = null;

                if (paths == null || paths.Length < 1)
                {
                    if (!string.IsNullOrWhiteSpace(x.Value))
                    {
                        value = x.Value;
                        epc = IdentifierFactory.ParseEPC(value, out error);
                    }
                }
                else
                {
                    foreach (string path in paths)
                    {
                        if (!string.IsNullOrWhiteSpace(x[path].Value))
                        {
                            value = x[path].Value;
                            epc = IdentifierFactory.ParseEPC(value, out error);
                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new TEMappingException($"Failed to parse the EPC. {error}. epc= " + value);
                }

                return epc;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private IGLN TryParseGLN(TEXML x, params string[] paths)
        {
            try
            {
                IGLN gln = null;
                string error = null;
                string value = null;

                if (paths == null || paths.Length < 1)
                {
                    if (!string.IsNullOrWhiteSpace(x.Value))
                    {
                        value = x.Value;
                        gln = IdentifierFactory.ParseGLN(value, out error);
                    }
                }
                else
                {
                    foreach (string path in paths)
                    {
                        if (!string.IsNullOrWhiteSpace(x[path].Value))
                        {
                            value = x[path].Value;
                            gln = IdentifierFactory.ParseGLN(value, out error);
                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new TEMappingException($"Failed to parse the EPC. {error}. epc= " + value);
                }

                return gln;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private IPGLN TryParsePGLN(TEXML x, params string[] paths)
        {
            try
            {
                IPGLN pgln = null;
                string error = null;
                string value = null;

                if (paths == null || paths.Length < 1)
                {
                    if (!string.IsNullOrWhiteSpace(x.Value))
                    {
                        value = x.Value;
                        pgln = IdentifierFactory.ParsePGLN(value, out error);
                    }
                }
                else
                {
                    foreach (string path in paths)
                    {
                        if (!string.IsNullOrWhiteSpace(x[path].Value))
                        {
                            value = x[path].Value;
                            pgln = IdentifierFactory.ParsePGLN(value, out error);
                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new TEMappingException($"Failed to parse the EPC. {error}. epc= " + value);
                }

                return pgln;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        #endregion
    }
}
