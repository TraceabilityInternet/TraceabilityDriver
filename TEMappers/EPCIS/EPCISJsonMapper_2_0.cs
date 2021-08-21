using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models;
using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Models;
using TraceabilityEngine.Models.Events;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.StaticData;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Mappers.EPCIS
{
    public class EPCISJsonMapper_2_0 : GS1WebVocabMapper, ITEEPCISMapper
    {
        /// <summary>
        /// This takes a list of events and maps them into the EPCIS 2.0 JSON format. At the end it also validates the mapping by using the 
        /// EPCIS 2.0 JSON Schema.
        /// </summary>
        /// <param name="ctes">A list of events.</param>
        /// <param name="cbvMappings">This is a list of mappings that are used to map non-CBV dispositions and business steps into CBV compliant values.</param>
        /// <exception cref="TEMappingException"></exception>
        /// <returns>A string JSON value of the EPCIS events.</returns>
        public string WriteEPCISData(ITEEPCISDocument data)
        {
            JObject json = new JObject();

            JArray jContext = new JArray();
            jContext.Add("https://gs1.github.io/EPCIS/epcis-context.jsonld");
            json["@context"] = jContext;

            //json["id"] = "_:document1";
            json["isA"] = "EPCISDocument";
            json["schemaVersion"] = "2.0";
            json["creationDate"] = DateTime.UtcNow;

            JObject epcisBody = new JObject();
            JArray jEventList = new JArray();
            foreach (ITEEvent cte in data.Events) 
            {
                JObject jCTE = new JObject();

                if (cte is ITEObjectEvent)
                {
                    jCTE["isA"] = "ObjectEvent";
                    jCTE["action"] = cte.Action.ToString();
                }
                else if (cte is ITETransformationEvent)
                {
                    jCTE["isA"] = "TransformationEvent";
                }
                else if (cte is ITEAggregationEvent)
                {
                    jCTE["isA"] = "AggregationEvent";
                    jCTE["action"] = cte.Action.ToString();
                }
                else if (cte is ITEAssociationEvent)
                {
                    jCTE["isA"] = "AssociationEvent";
                    jCTE["action"] = cte.Action.ToString();
                }
                else if (cte is ITETransactionEvent)
                {
                    jCTE["isA"] = "TransactionEvent";
                    jCTE["action"] = cte.Action.ToString();
                }

                if (!string.IsNullOrWhiteSpace(cte.EventID))
                {
                    jCTE["eventID"] = cte.EventID;
                }
                jCTE["eventTime"] = cte.EventTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK", CultureInfo.InvariantCulture);
                jCTE["eventTimeZoneOffset"] = ConvertToOffset(cte.EventTimeOffset);
                jCTE["cbvmda:informationProvider"] = cte.DataOwner?.ToString();
                jCTE["gdst:productOwner"] = cte.Owner?.ToString();

                if (!string.IsNullOrWhiteSpace(cte.BusinessStep))
                {
                    jCTE["bizStep"] = cte.BusinessStep;
                }

                if (!string.IsNullOrWhiteSpace(cte.Disposition))
                {
                    jCTE["disposition"] = cte.Disposition;
                }

                if (cte.PersistentDisposition != null)
                {
                    JObject jPerDisp = new JObject();
                    if (cte.PersistentDisposition.Set != null && cte.PersistentDisposition.Set.Count > 0)
                    {
                        JArray jSet = new JArray();
                        foreach(string disp in cte.PersistentDisposition.Set)
                        {
                            jSet.Add(disp);
                        }
                        jPerDisp["set"] = jSet;
                    }
                    if (cte.PersistentDisposition.Unset != null && cte.PersistentDisposition.Unset.Count > 0)
                    {
                        JArray jUnset = new JArray();
                        foreach (string disp in cte.PersistentDisposition.Unset)
                        {
                            jUnset.Add(disp);
                        }
                        jPerDisp["unset"] = jUnset;
                    }
                    jCTE["persistentDisposition"] = jPerDisp;
                }

                if (cte.ReadPoint != null)
                {
                    JObject jReadPoint = new JObject();
                    jReadPoint["id"] = cte.ReadPoint.ID;
                    jCTE["readPoint"] = jReadPoint;
                }

                if (cte.Location?.GLN != null)
                {
                    JObject jLocation = new JObject();
                    jLocation["id"] = cte.Location.GLN.ToString();
                    jCTE["bizLocation"] = jLocation;
                }

                if (cte.BusinessTransactions != null && cte.BusinessTransactions.Count > 0)
                {
                    JArray jBizTransactions = new JArray();
                    foreach (ITEEventBusinessTransaction bizTransaction in cte.BusinessTransactions)
                    {
                        JObject jBizTransaction = new JObject();
                        jBizTransaction["id"] = bizTransaction.RawType;
                        jBizTransaction["bizTransaction"] = bizTransaction.Value;
                        jBizTransactions.Add(jBizTransaction);
                    }
                    jCTE["bizTransactionList"] = jBizTransactions;
                }

                if (cte.SourceList != null && cte.SourceList.Count > 0)
                {
                    JArray jSourceList = new JArray();
                    foreach(ITEEventSource source in cte.SourceList)
                    {
                        JObject jSource = new JObject();
                        jSource["type"] = source.RawType;
                        jSource["source"] = source.Value;
                        jSourceList.Add(jSource);
                    }
                    jCTE["sourceList"] = jSourceList;
                }

                if (cte.DestinationList != null && cte.DestinationList.Count > 0)
                {
                    JArray jDestinationList = new JArray();
                    foreach (ITEEventDestination destination in cte.DestinationList)
                    {
                        JObject jDestination = new JObject();
                        jDestination["type"] = destination.RawType;
                        jDestination["destination"] = destination.Value;
                        jDestinationList.Add(jDestination);
                    }
                    jCTE["destinationList"] = jDestinationList;
                }                

                if (cte.SensorElementList != null && cte.SensorElementList.Count > 0)
                {
                    JArray jSensorElementList = new JArray();
                    foreach (ITESensorElement element in cte.SensorElementList)
                    {
                        JObject jSensorElement = new JObject();
                        jSensorElement["isA"] = "epcis:SensorElement";

                        JObject jMetaData = new JObject();
                        jMetaData["time"] = element.TimeStamp?.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK", CultureInfo.InvariantCulture);
                        jMetaData["deviceID"] = element.DeviceID;
                        jMetaData["deviceMetaData"] = element.DeviceMetaData;
                        jMetaData["rawData"] = element.RawData;
                        jSensorElement["sensorMetadata"] = jMetaData;

                        if (element.Reports != null && element.Reports.Count > 0)
                        {
                            JArray jReports = new JArray();
                            foreach (ITESensorReport report in element.Reports)
                            {
                                JObject jReport = new JObject();
                                jReport["type"] = report.Type;
                                if (report.TimeStamp != null)
                                {
                                    jReport["time"] = report.TimeStamp?.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK", CultureInfo.InvariantCulture);
                                }
                                if (report.Measurement != null)
                                {
                                    jReport["value"] = report.Measurement.Value;
                                    jReport["uom"] = report.Measurement.UoM?.UNCode;
                                }

                                jReports.Add(jReport);
                            }
                            jSensorElement["sensorReport"] = jReports;
                        }

                        jSensorElementList.Add(jSensorElement);
                    }
                    jCTE["sensorElementList"] = jSensorElementList;
                }

                if (cte.ErrorDeclaration != null)
                {
                    JObject jErrorDeclaration = new JObject();
                    jErrorDeclaration["declarationTime"] = cte.ErrorDeclaration.DeclarationTime;
                    jErrorDeclaration["reason"] = cte.ErrorDeclaration.RawReason;
                    if (cte.ErrorDeclaration.CorrectingEventIDs != null && cte.ErrorDeclaration.CorrectingEventIDs.Count > 0)
                    {
                        JArray jCorrectiveIDs = new JArray();
                        foreach (string id in cte.ErrorDeclaration.CorrectingEventIDs)
                        {
                            jCorrectiveIDs.Add(id);
                        }
                        jErrorDeclaration["correctiveEventIDs"] = jCorrectiveIDs;
                    }
                }

                // ILMD
                if (cte.ILMD != null)
                {
                    JObject jILMD = new JObject();

                    jILMD["cbvmda:expirationDate"] = cte.ILMD.ExpirationDate;
                    jILMD["cbvmda:lotNumber"] = cte.ILMD.LotNumber;

                    // add the extension KDEs
                    if(cte.ILMD.KDEs != null && cte.ILMD.KDEs.Count > 0)
                    {
                        foreach (ITEEventKDE kde in cte.KDEs)
                        {
                            string name = kde.Name;
                            if (!string.IsNullOrWhiteSpace(kde.NamespacePrefix))
                            {
                                name = kde.NamespacePrefix + ":" + name;
                            }
                            jILMD[name] = kde.JsonValue;
                        }
                    }

                    jCTE["ilmd"] = jILMD;
                }

                // products
                foreach (ITEEventProduct product in cte.Products)
                {
                    if (IEPC.IsNullOrEmpty(product.EPC))
                    {
                        throw new TEMappingException($"Failed to map cte {cte.EventID} into EPCIS JSON 2.0. It has a product with a NULL or EMPTY EPC.");
                    }

                    string epcListName = "epcList";
                    string quantityListName = "quantityList";

                    if (product.Type == EventProductType.Parent)
                    {
                        jCTE["parentID"] = product.EPC.ToString();
                        continue;
                    }
                    else if (product.Type == EventProductType.Child)
                    {
                        epcListName = "childEPCs";
                        quantityListName = "childQuantityList";
                    }
                    else if (product.Type == EventProductType.Input)
                    {
                        epcListName = "inputEPCList";
                        quantityListName = "inputQuantityList";
                    }
                    else if (product.Type == EventProductType.Output)
                    {
                        epcListName = "outputEPCList";
                        quantityListName = "outputQuantityList";
                    }

                    if (product.EPC.Type == EPCType.Instance)
                    {
                        if (jCTE[epcListName] == null)
                        {
                            jCTE[epcListName] = new JArray();
                        }
                            (jCTE[epcListName] as JArray).Add(product.EPC.ToString());
                    }
                    else if (product.EPC.Type == EPCType.Class)
                    {
                        if (jCTE[quantityListName] == null)
                        {
                            jCTE[quantityListName] = new JArray();
                        }
                        JObject jQuantity = new JObject();
                        jQuantity["epcClass"] = product.EPC.ToString();
                        if (product.Quantity != null)
                        {
                            jQuantity["quantity"] = product.Quantity.Value;
                            if (product.Quantity.UoM != null && product.Quantity.UoM.UNCode != "EA")
                            {
                                jQuantity["uom"] = product.Quantity.UoM.UNCode;
                            }
                        }
                        (jCTE[quantityListName] as JArray).Add(jQuantity);
                    }
                }

                // certificates
                cte.Certificates = GetCertificates(jCTE, "gs1:certification");

                // attachments
                cte.Attachments = GetAttachments(jCTE, "gs1:referencedFile");

                // custom KDEs
                if (cte.KDEs != null && cte.KDEs.Count > 0)
                {
                    foreach (ITEEventKDE kde in cte.KDEs)
                    {
                        string name = kde.Name;
                        if (!string.IsNullOrWhiteSpace(kde.NamespacePrefix))
                        {
                            name = kde.NamespacePrefix + ":" + name;
                        }
                        jCTE[name] = kde.JsonValue;
                    }
                }

                jEventList.Add(jCTE);
            }
            epcisBody["eventList"] = jEventList;
            json["epcisBody"] = epcisBody;

            ValidateJson(json);

            return json.ToString();
        }

        /// <summary>
        /// Converts a JSON EPCIS 2.0 Document to a list of events.
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public ITEEPCISDocument ReadEPCISData(string jsonStr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonStr)) throw new ArgumentNullException(nameof(jsonStr));

                JObject json = null;
                try
                {
                    json = JObject.Parse(jsonStr);
                }
                catch (Exception Ex)
                {
                    throw new TEMappingException("Failed to parse the JSON.", Ex);
                }
                if (json == null)
                {
                    throw new TEMappingException("Failed to parse the JSON. After parsing the JObject was NULL.");
                }

                ValidateJson(json);

                ITEEPCISDocument data = new TEEPCISDocument();
                if (json["epcisBody"] != null)
                {
                    if (json["epcisBody"]["eventList"] != null && json["epcisBody"]["eventList"] is JArray)
                    {
                        JArray jCTEs = json["epcisBody"]["eventList"] as JArray;
                        foreach (JObject jCTE in jCTEs)
                        {
                            ITEEvent cte = null;

                            string eventType = jCTE["isA"].ToString();
                            switch (eventType)
                            {
                                case "ObjectEvent":
                                    {
                                        cte = new TEObjectEvent();
                                        cte.Action = (TEEventAction)Enum.Parse(typeof(TEEventAction), jCTE["action"].Value<string>());
                                        break;
                                    }
                                case "TransactionEvent":
                                    {
                                        cte = new TETransactionEvent();
                                        cte.Action = (TEEventAction)Enum.Parse(typeof(TEEventAction), jCTE["action"].Value<string>());
                                        break;
                                    }
                                case "TransformationEvent":
                                    {
                                        cte = new TETransformationEvent();
                                        break;
                                    }
                                case "AggregationEvent":
                                    {
                                        cte = new TEAggregationEvent();
                                        cte.Action = (TEEventAction)Enum.Parse(typeof(TEEventAction), jCTE["action"].Value<string>());
                                        break;
                                    }
                                case "AssociationEvent":
                                    {
                                        cte = new TEAssociationEvent();
                                        cte.Action = (TEEventAction)Enum.Parse(typeof(TEEventAction), jCTE["action"].Value<string>());
                                        break;
                                    }
                                default:
                                    {
                                        throw new TEMappingException($"The eventType '{eventType}' is not recognized.");
                                    }
                            }

                            if (jCTE["eventID"] != null)
                            {
                                cte.EventID = jCTE["eventID"].Value<string>();
                            }

                            cte.EventTime = jCTE["eventTime"].Value<DateTime>();
                            cte.EventTimeOffset = ConvertFromOffset(jCTE["eventTimeZoneOffset"]?.Value<string>() ?? "+00:00");
                            cte.BusinessStep = jCTE["bizStep"]?.Value<string>();
                            cte.Disposition = jCTE["disposition"]?.Value<string>();
                            if (jCTE["persistentDisposition"] != null)
                            {
                                cte.PersistentDisposition = new TEPersistentDisposition();
                                if (jCTE["persistentDisposition"]["set"] != null)
                                {
                                    cte.PersistentDisposition.Set = new List<string>();
                                    foreach (JToken jT in jCTE["persistentDisposition"]["set"])
                                    {
                                        string set = jT.ToString();
                                        cte.PersistentDisposition.Set.Add(set);
                                    }
                                }
                                if (jCTE["persistentDisposition"]["unset"] != null)
                                {
                                    cte.PersistentDisposition.Unset = new List<string>();
                                    foreach (JToken jT in jCTE["persistentDisposition"]["unset"])
                                    {
                                        string unset = jT.ToString();
                                        cte.PersistentDisposition.Unset.Add(unset);
                                    }
                                }
                            }

                            cte.DataOwner = IdentifierFactory.ParsePGLN(jCTE["cbvmda:informationProvider"]?.Value<string>());
                            cte.Owner = IdentifierFactory.ParsePGLN(jCTE["gdst:productOwner"]?.Value<string>());

                            if (jCTE["readPoint"]?["id"] != null)
                            {
                                cte.ReadPoint = new TEEventReadPoint();
                                cte.ReadPoint.ID = jCTE["readPoint"]["id"].Value<string>();
                            }

                            if (jCTE["bizLocation"]?["id"] != null)
                            {
                                cte.Location = new TEEventLocation();
                                cte.Location.GLN = IdentifierFactory.ParseGLN(jCTE["bizLocation"]["id"].Value<string>());
                            }

                            if (jCTE["sourceList"] != null && jCTE["sourceList"] is JArray)
                            {
                                cte.SourceList = new List<ITEEventSource>();
                                foreach (JObject jSource in jCTE["sourceList"])
                                {
                                    TEEventSource source = new TEEventSource();
                                    source.RawType = jSource["type"].Value<string>();
                                    source.Value = jSource["source"].Value<string>();
                                    cte.SourceList.Add(source);
                                }
                            }

                            if (jCTE["destinationList"] != null && jCTE["destinationList"] is JArray)
                            {
                                cte.DestinationList = new List<ITEEventDestination>();
                                foreach (JObject jDestination in jCTE["destinationList"])
                                {
                                    TEEventDestination dest = new TEEventDestination();
                                    dest.RawType = jDestination["type"].Value<string>();
                                    dest.Value = jDestination["destination"].Value<string>();
                                    cte.DestinationList.Add(dest);
                                }
                            }

                            if (jCTE["bizTransactionList"] != null && jCTE["bizTransactionList"] is JArray)
                            {
                                cte.BusinessTransactions = new List<ITEEventBusinessTransaction>();
                                foreach (JObject jBizTransaction in jCTE["bizTransactionList"])
                                {
                                    TEEventBusinessTransaction bizTransaction = new TEEventBusinessTransaction();
                                    bizTransaction.RawType = jBizTransaction["type"].Value<string>();
                                    bizTransaction.Value = jBizTransaction["bizTransaction"].Value<string>();
                                    cte.BusinessTransactions.Add(bizTransaction);
                                }
                            }

                            if (jCTE["errorDeclaration"] != null)
                            {
                                JObject jErrorDeclaration = jCTE["errorDeclaration"] as JObject;
                                cte.ErrorDeclaration = new TEErrorDeclaration();
                                cte.ErrorDeclaration.DeclarationTime = jErrorDeclaration.Value<DateTime>("declarationTime");
                                cte.ErrorDeclaration.RawReason = jErrorDeclaration.Value<string>("reason");

                                if (jErrorDeclaration["correctiveEventIDs"] != null && jErrorDeclaration["correctiveEventIDs"] is JArray)
                                {
                                    cte.ErrorDeclaration.CorrectingEventIDs = new List<string>();
                                    JArray jCorrectiveIDs = jErrorDeclaration["correctiveEventIDs"] as JArray;
                                    foreach (JToken jID in jCorrectiveIDs)
                                    {
                                        cte.ErrorDeclaration.CorrectingEventIDs.Add(jID.Value<string>());
                                    }
                                }
                            }

                            // sensor report list
                            cte.SensorElementList = new List<ITESensorElement>();
                            if (jCTE["sensorElementList"] != null && jCTE["sensorElementList"] is JArray)
                            {
                                foreach (JObject jSensor in jCTE["sensorElementList"])
                                {
                                    TESensorElement element = new TESensorElement();
                                    element.DeviceID = GetUri(jSensor["sensorMetadata"], "deviceID");
                                    element.DeviceMetaData = jSensor["sensorMetadata"]?.Value<string>("deviceMetaData");
                                    element.RawData = GetUri(jSensor["sensorMetadata"], "rawData");
                                    element.TimeStamp = jSensor["sensorMetadata"]?.Value<DateTime?>("time");
                                    element.Reports = new List<ITESensorReport>();
                                    if (jSensor["sensorReport"] != null && jSensor["sensorReport"] is JArray)
                                    {
                                        foreach (JObject jReport in jSensor["sensorReport"])
                                        {
                                            TESensorReport report = new TESensorReport();
                                            report.Type = GetUri(jReport, "type");
                                            report.TimeStamp = jReport["time"]?.Value<DateTime?>();

                                            double value = jReport["value"]?.Value<double>() ?? 0;
                                            string uom = jReport["uom"]?.Value<string>() ?? "";
                                            report.Measurement = new TraceabilityEngine.Util.StaticData.TEMeasurement(value, uom);
                                            element.Reports.Add(report);
                                        }
                                    }
                                    cte.SensorElementList.Add(element);
                                }
                            }

                            // ilmd
                            if (jCTE["ilmd"] != null)
                            {
                                cte.ILMD = new TEILMD();
                                JObject jILMD = jCTE["ilmd"] as JObject;

                                // kdes
                                cte.ILMD.KDEs = new List<ITEEventKDE>();
                                foreach (JProperty jProp in jILMD.Properties())
                                {
                                    string name = jProp.Name;
                                    ITEEventKDE kde = ITEEventKDE.InitializeFromKey(name);
                                    if (kde != null)
                                    {
                                        kde.JsonValue = jCTE[name];
                                        cte.ILMD.KDEs.Add(kde);
                                    }
                                }
                            }

                            // certifications
                            cte.Certificates = GetCertificates(jCTE, "gs1:certification");

                            // attachments
                            cte.Attachments = GetAttachments(jCTE, "gs1:referencedFile");

                            // products
                            if (jCTE["outputEPCList"] is JArray && cte is ITETransformationEvent)
                            {
                                ITETransformationEvent transformEvent = (ITETransformationEvent)cte;
                                foreach (JToken jEPC in jCTE["outputEPCList"])
                                {
                                    string epcStr = jEPC.Value<string>();
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from outputEPCList on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Instance;
                                    transformEvent.AddOutput(epc);
                                }
                            }

                            if (jCTE["inputEPCList"] is JArray && cte is ITETransformationEvent)
                            {
                                ITETransformationEvent transformEvent = (ITETransformationEvent)cte;
                                foreach (JToken jEPC in jCTE["inputEPCList"])
                                {
                                    string epcStr = jEPC.Value<string>();
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from inputEPCList on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Instance;
                                    transformEvent.AddInput(epc);
                                }
                            }

                            if (jCTE["epcList"] is JArray && cte is ITEObjectEvent)
                            {
                                ITEObjectEvent objEvent = (ITEObjectEvent)cte;
                                foreach (JToken jEPC in jCTE["epcList"])
                                {
                                    string epcStr = jEPC.Value<string>();
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from epcList on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Instance;
                                    objEvent.AddProduct(epc);
                                }
                            }

                            if (jCTE["epcList"] is JArray && cte is ITETransactionEvent)
                            {
                                ITETransactionEvent transactionEvent = (ITETransactionEvent)cte;
                                foreach (JToken jEPC in jCTE["epcList"])
                                {
                                    string epcStr = jEPC.Value<string>();
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from epcList on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Instance;
                                    transactionEvent.AddProduct(epc);
                                }
                            }

                            if (jCTE["childEPCs"] is JArray && cte is ITEAssociationEvent)
                            {
                                ITEAssociationEvent associationEvent = (ITEAssociationEvent)cte;
                                foreach (JToken jEPC in jCTE["childEPCs"])
                                {
                                    string epcStr = jEPC.Value<string>();
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from childEPCs on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Instance;
                                    associationEvent.AddChild(epc);
                                }
                            }

                            if (jCTE["childEPCs"] is JArray && cte is ITEAggregationEvent)
                            {
                                ITEAggregationEvent aggEvent = (ITEAggregationEvent)cte;
                                foreach (JToken jEPC in jCTE["childEPCs"])
                                {
                                    string epcStr = jEPC.Value<string>();
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from childEPCs on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Instance;
                                    aggEvent.AddChild(epc);
                                }
                            }

                            if (jCTE["outputQuantityList"] is JArray && cte is ITETransformationEvent)
                            {
                                ITETransformationEvent transformEvent = (ITETransformationEvent)cte;
                                foreach (JObject jQuantity in jCTE["outputQuantityList"])
                                {
                                    string epcStr = jQuantity.Value<string>("epcClass");
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from outputQuantityList on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Class;
                                    double quantity = jQuantity.Value<double>("quantity");
                                    string uom = jQuantity.Value<string>("uom") ?? "EA";
                                    transformEvent.AddOutput(epc, quantity, uom);
                                }
                            }

                            if (jCTE["inputQuantityList"] is JArray && cte is ITETransformationEvent)
                            {
                                ITETransformationEvent transformEvent = (ITETransformationEvent)cte;
                                foreach (JObject jQuantity in jCTE["inputQuantityList"])
                                {
                                    string epcStr = jQuantity.Value<string>("epcClass");
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from inputQuantityList on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Class;
                                    double quantity = jQuantity.Value<double>("quantity");
                                    string uom = jQuantity.Value<string>("uom") ?? "EA";
                                    transformEvent.AddInput(epc, quantity, uom);
                                }
                            }

                            if (jCTE["quantityList"] is JArray && cte is ITEObjectEvent)
                            {
                                ITEObjectEvent objEvent = (ITEObjectEvent)cte;
                                foreach (JObject jQuantity in jCTE["quantityList"])
                                {
                                    string epcStr = jQuantity.Value<string>("epcClass");
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from quantityList on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Class;
                                    double quantity = jQuantity.Value<double>("quantity");
                                    string uom = jQuantity.Value<string>("uom") ?? "EA";
                                    objEvent.AddProduct(epc, quantity, uom);
                                }
                            }

                            if (jCTE["quantityList"] is JArray && cte is ITETransactionEvent)
                            {
                                ITETransactionEvent transactionEvent = (ITETransactionEvent)cte;
                                foreach (JObject jQuantity in jCTE["quantityList"])
                                {
                                    string epcStr = jQuantity.Value<string>("epcClass");
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from quantityList on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Class;
                                    double quantity = jQuantity.Value<double>("quantity");
                                    string uom = jQuantity.Value<string>("uom") ?? "EA";
                                    transactionEvent.AddProduct(epc, quantity, uom);
                                }
                            }

                            if (jCTE["childQuantityList"] is JArray && cte is ITEAssociationEvent)
                            {
                                ITEAssociationEvent assEvent = (ITEAssociationEvent)cte;
                                foreach (JObject jQuantity in jCTE["childQuantityList"])
                                {
                                    string epcStr = jQuantity.Value<string>("epcClass");
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from childQuantityList on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Class;
                                    double quantity = jQuantity.Value<double>("quantity");
                                    string uom = jQuantity.Value<string>("uom") ?? "EA";
                                    assEvent.AddChild(epc, quantity, uom);
                                }
                            }

                            if (jCTE["childQuantityList"] is JArray && cte is ITEAggregationEvent)
                            {
                                ITEAggregationEvent aggEvent = (ITEAggregationEvent)cte;
                                foreach (JObject jQuantity in jCTE["childQuantityList"])
                                {
                                    string epcStr = jQuantity.Value<string>("epcClass");
                                    if (!EPC.TryParse(epcStr, out IEPC epc, out string error))
                                    {
                                        throw new TEMappingException($"Failed to parse EPC {epcStr} from childQuantityList on event {cte.EventID}.");
                                    }
                                    epc.Type = EPCType.Class;
                                    double quantity = jQuantity.Value<double>("quantity");
                                    string uom = jQuantity.Value<string>("uom") ?? "EA";
                                    aggEvent.AddChild(epc, quantity, uom);
                                }
                            }

                            if (jCTE["parentID"] != null)
                            {
                                string parentID = jCTE.Value<string>("parentID");
                                if (!EPC.TryParse(parentID, out IEPC epc, out string error))
                                {
                                    throw new TEMappingException($"Failed to parse Parent ID {parentID} from parentID on event {cte.EventID}.");
                                }
                                if (cte is ITEAssociationEvent)
                                {
                                    (cte as ITEAssociationEvent).ParentID = epc;
                                }
                                else if (cte is ITEAggregationEvent)
                                {
                                    (cte as ITEAggregationEvent).ParentID = epc;
                                }
                            }

                            // kdes
                            foreach (JProperty jProp in jCTE.Properties())
                            {
                                string name = jProp.Name;
                                ITEEventKDE kde = ITEEventKDE.InitializeFromKey(name);
                                if (kde != null)
                                {
                                    kde.JsonValue = jCTE[name];
                                    cte.KDEs.Add(kde);
                                }
                            }

                            data.Events.Add(cte);
                        }
                    }
                }

                return data;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        /// <summary>
        /// This validates that the JSON meets the schema requirements for EPCIS 2.0. It throws exceptions if it does not meet the requirements.
        /// </summary>
        /// <exception cref="TEMappingException"></exception>
        /// <param name="json">The JSON to be validated.</param>
        private void ValidateJson(JObject json)
        {
            // run the json through the schema to ensure it is correct
            string jsonSchemaPath = "TraceabilityEngine.Mappers.EPCIS.EPCIS-JSON-Schema_2_0.json";
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            string jsonSchemaString = loader.ReadString("TEMappers", jsonSchemaPath);
            if (string.IsNullOrWhiteSpace(jsonSchemaString))
            {
                throw new TEMappingException($"Failed to load the JSON Schema from the embedded resources. The path used was ${jsonSchemaPath}");
            }
            JSchema schema = null;
            try
            {
                schema = JSchema.Parse(jsonSchemaString);
            }
            catch (Exception Ex)
            {
                throw new TEMappingException("There was an unknown exception parsing the JSON Schema.", Ex);
            }
            if (schema == null)
            {
                throw new TEMappingException("There was an unknown exception parsing the JSON Schema. The JSON Schema is NULL after trying to parse from a string.");
            }
            if (!json.IsValid(schema, out IList<string> errors))
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    string jsonStr = json.ToString();
                    Debugger.Break();
                }
#endif
                throw new TEMappingException($@"The JSON failed the Schema Validation. Errors:\n{String.Join("\n", errors)}");
            }
        }

        private string ConvertToOffset(double offset)
        {
            if (offset < -12 || offset > 12)
            {
                throw new TEMappingException($"The offset must be greater than or equal to -12 or less than and equal to 12. The offset value {offset} is not in this range.");
            }
            int minutes = (int)Math.Floor(((Math.Abs(offset) - Math.Floor(Math.Abs(offset))) * 60));
            int hours = (int)Math.Abs(Math.Floor(offset));

            string hoursStr = hours.ToString().PadLeft(2, '0');
            string minutesStr = minutes.ToString().PadLeft(2, '0');
            string offsetStr = hoursStr + ":" + minutesStr;
            if (offset < 0)
            {
                offsetStr = "-" + offsetStr;
            }
            else
            {
                offsetStr = "+" + offsetStr;    
            }
            return offsetStr;
        }

        private double ConvertFromOffset(string offsetStr)
        {
            double offset = 0;
            if (!string.IsNullOrWhiteSpace(offsetStr))
            {
                string[] pieces = offsetStr.Split(':');
                if (pieces.Length == 2)
                {
                    double hours = double.Parse(pieces[0]);
                    double minutes = double.Parse(pieces[1]);
                    if (hours < 0)
                    {
                        hours -= minutes;
                    }
                    else
                    {
                        hours += minutes;
                    }
                    offset = hours;
                }
            }
            return offset;
        }
    }
}
