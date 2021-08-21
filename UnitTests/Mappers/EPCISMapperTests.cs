using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Models;
using TraceabilityEngine.Models.Events;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.StaticData;

namespace UnitTests.Mappers
{
    [TestClass]
    public class EPCISMapperTests
    {
        private List<ITEEvent> events;

        [TestInitialize]
        public void Initialize()
        {
            events = new List<ITEEvent>();

            IGLN metalMine = new GLN("urn:epc:id:sgln:08600031303.0");
            IGLN processingPlant = new GLN("urn:epc:id:sgln:08600031303.1");
            IGLN secondaryProcessingPlant = new GLN("urn:epc:id:sgln:08600031303.2");
            IGLN trainYard = new GLN("urn:epc:id:sgln:08600031303.3");

            IEPC metalOre = new EPC("urn:epc:class:lgtin:08600031303.00.METAL");
            IEPC steel = new EPC("urn:epc:class:lgtin:08600031303.01.STEEL");
            IEPC wheel1 = new EPC("urn:epc:id:sgtin:08600031303.02.WHEEL01");
            IEPC wheel2 = new EPC("urn:epc:id:sgtin:08600031303.02.WHEEL02");
            IEPC wheel3 = new EPC("urn:epc:id:sgtin:08600031303.02.WHEEL03");
            IEPC wheel4 = new EPC("urn:epc:id:sgtin:08600031303.02.WHEEL04");
            IEPC wheel5 = new EPC("urn:epc:id:sgtin:08600031303.02.WHEEL05");
            IEPC wheel6 = new EPC("urn:epc:id:sgtin:08600031303.02.WHEEL06");
            IEPC wheel7 = new EPC("urn:epc:id:sgtin:08600031303.02.WHEEL07");
            IEPC wheel8 = new EPC("urn:epc:id:sgtin:08600031303.02.WHEEL08");
            IEPC wheel9 = new EPC("urn:epc:id:sgtin:08600031303.02.WHEEL09");
            IEPC wheel10 = new EPC("urn:epc:id:sgtin:08600031303.02.WHEEL10");
            IEPC train = new EPC("urn:epc:id:sgtin:08600031303.03.TRAIN");

            IPGLN metalMineOwner = new PGLN("urn:epc:id:sgln:08600031303.4");
            IPGLN processor1 = new PGLN("urn:epc:id:sgln:08600031303.5");
            IPGLN processor2 = new PGLN("urn:epc:id:sgln:08600031303.6");
            IPGLN trainOwner = new PGLN("urn:epc:id:sgln:08600031303.7");            

            IEPC sscc = new EPC("urn:sscc:1234567890");

            TEAttachment dummyAttachment = new TEAttachment()
            {
                AttachmentType = AttachmentType.GetFromKey("gs1:ReferencedFileTypeCode-DIET_CERTIFICATE"),
                Description = "test",
                FileName = "test.pdf",
                URL = new Uri("http://www.google.com"),
                URI = new Uri("urn:uuid:1234567890"),
                PixelHeight = 123,
                PixelWidth = 123,
                Size = new TraceabilityEngine.Util.StaticData.TEMeasurement(123, "KGM")
            };

            // create
            {
                ITEObjectEvent createEvent = new TEObjectEvent();
                createEvent.EventTime = DateTime.UtcNow;
                createEvent.EventTimeOffset = 1;
                createEvent.DataOwner = metalMineOwner;
                createEvent.Owner = metalMineOwner;
                createEvent.Action = TEEventAction.ADD;
                createEvent.BusinessStep = "urn:epcglobal:cbv:bizstep:commissioning";
                createEvent.Disposition = "urn:epcglobal:cbv:disp:in_progress";
                createEvent.PersistentDisposition = new TEPersistentDisposition()
                {
                    Set = new List<string>() { "urn:epcglobal:cbv:disp:in_progress" },
                    Unset = new List<string>() { "urn:epcglobal:cbv:disp:in_progress" }
                };
                createEvent.Location = new TEEventLocation(metalMine);
                createEvent.AddProduct(metalOre, 100, "KGM");
                createEvent.Certificates.Add(new TECertificate()
                {
                    Agency = "test",
                    AgencyURL = new Uri("https://www.google.com"),
                    Attachment = dummyAttachment,
                    AuditDate = DateTime.Now,
                    CertificateType = CertificateType.GetFromKey("urn:gdst:certType:aggregatorLicense"),
                    EndDate = DateTime.Now,
                    StartDate = DateTime.Now,
                    Identification = "test",
                    Standard = "test",
                    Subject = "test",
                    URI = new Uri("urn:uuid:1234567890"),
                    Value = "test"
                });
                createEvent.Attachments.Add(dummyAttachment);
                createEvent.SensorElementList.Add(new TESensorElement()
                {
                    DeviceID = new Uri("https://www.google.com"),
                    DeviceMetaData = "123",
                    RawData = new Uri("https://www.google.com"),
                    TimeStamp = DateTime.Now,
                    Reports = new List<ITESensorReport>()
                    {
                        new TESensorReport()
                        {
                            Measurement = new TraceabilityEngine.Util.StaticData.TEMeasurement(123, "KGM"),
                            Type = new Uri("https://www.google.com")
                        }
                    }
                });
                events.Add(createEvent);
            }

            // receive
            {
                ITEObjectEvent receiveEvent = new TEObjectEvent();
                receiveEvent.EventTime = DateTime.UtcNow.AddDays(1);
                receiveEvent.EventTimeOffset = 1;
                receiveEvent.Owner = processor1;
                receiveEvent.DataOwner = processor1;
                receiveEvent.Action = TEEventAction.OBSERVE;
                receiveEvent.BusinessStep = "urn:epcglobal:cbv:bizstep:receiving";
                receiveEvent.Disposition = "urn:epcglobal:cbv:disp:in_progress";
                receiveEvent.Location = new TEEventLocation(processingPlant);
                receiveEvent.AddProduct(metalOre, 100, "KGM");
                receiveEvent.AddBuyer(processor1);
                receiveEvent.AddSeller(metalMineOwner);
                events.Add(receiveEvent);
            }

            // transformation
            {
                ITETransformationEvent transformEvent = new TETransformationEvent();
                transformEvent.EventTime = DateTime.UtcNow.AddDays(2);
                transformEvent.EventTimeOffset = 1;
                transformEvent.Owner = processor1;
                transformEvent.DataOwner = processor1;
                transformEvent.Action = TEEventAction.ADD;
                transformEvent.BusinessStep = "urn:epcglobal:cbv:bizstep:commissioning";
                transformEvent.Disposition = "urn:epcglobal:cbv:disp:active";
                transformEvent.Location = new TEEventLocation(processingPlant);
                transformEvent.AddInput(metalOre, 100, "KGM");
                transformEvent.AddOutput(steel, 75, "KGM");
                events.Add(transformEvent);
            }

            // aggregate
            {
                ITEAggregationEvent aggEvent = new TEAggregationEvent();
                aggEvent.EventTime = DateTime.UtcNow.AddDays(3);
                aggEvent.EventTimeOffset = 1;
                aggEvent.Owner = processor1;
                aggEvent.DataOwner = processor1;
                aggEvent.Action = TEEventAction.ADD;
                aggEvent.BusinessStep = "urn:epcglobal:cbv:bizstep:commissioning";
                aggEvent.Location = new TEEventLocation(processingPlant);
                aggEvent.ParentID = sscc;
                aggEvent.AddChild(steel, 75, "KGM");
                events.Add(aggEvent);
            }

            // ship
            {
                ITEObjectEvent shipEvent1 = new TEObjectEvent();
                shipEvent1.EventTime = DateTime.UtcNow.AddDays(4);
                shipEvent1.EventTimeOffset = 1;
                shipEvent1.Owner = processor1;
                shipEvent1.DataOwner = processor1;
                shipEvent1.Action = TEEventAction.OBSERVE;
                shipEvent1.BusinessStep = "urn:epcglobal:cbv:bizstep:shipping";
                shipEvent1.Disposition = "urn:epcglobal:cbv:disp:in_progress";
                shipEvent1.Location = new TEEventLocation(processingPlant);
                shipEvent1.AddProduct(steel, 75, "KGM");
                shipEvent1.AddBuyer(processor2);
                shipEvent1.AddSeller(processor1);
                events.Add(shipEvent1);
            }

            // receive
            {
                ITEObjectEvent receiveEvent2 = new TEObjectEvent();
                receiveEvent2.EventTime = DateTime.UtcNow.AddDays(5);
                receiveEvent2.EventTimeOffset = 1;
                receiveEvent2.Owner = processor1;
                receiveEvent2.DataOwner = processor1;
                receiveEvent2.Action = TEEventAction.OBSERVE;
                receiveEvent2.BusinessStep = "urn:epcglobal:cbv:bizstep:shipping";
                receiveEvent2.Disposition = "urn:epcglobal:cbv:disp:in_progress";
                receiveEvent2.Location = new TEEventLocation(secondaryProcessingPlant);
                receiveEvent2.AddProduct(steel, 75, "KGM");
                events.Add(receiveEvent2);
            }

            // disaggregate
            {
                ITEAggregationEvent disAggEvent = new TEAggregationEvent();
                disAggEvent.EventTime = DateTime.UtcNow.AddDays(6);
                disAggEvent.EventTimeOffset = 1;
                disAggEvent.Owner = processor1;
                disAggEvent.DataOwner = processor1;
                disAggEvent.Action = TEEventAction.DELETE;
                disAggEvent.BusinessStep = "urn:epcglobal:cbv:bizstep:decommissioning";
                disAggEvent.Location = new TEEventLocation(secondaryProcessingPlant);
                disAggEvent.ParentID = sscc;
                events.Add(disAggEvent);
            }

            // transform 
            {
                ITETransformationEvent transformEvent = new TETransformationEvent();
                transformEvent.EventTime = DateTime.UtcNow.AddDays(7);
                transformEvent.EventTimeOffset = 1;
                transformEvent.Owner = processor1;
                transformEvent.DataOwner = processor1;
                transformEvent.Action = TEEventAction.ADD;
                transformEvent.BusinessStep = "urn:epcglobal:cbv:bizstep:commissioning";
                transformEvent.Disposition = "urn:epcglobal:cbv:disp:active";
                transformEvent.Location = new TEEventLocation(secondaryProcessingPlant);
                transformEvent.AddInput(steel, 75, "KGM");
                transformEvent.AddOutput(wheel1);
                transformEvent.AddOutput(wheel2);
                transformEvent.AddOutput(wheel3);
                transformEvent.AddOutput(wheel4);
                transformEvent.AddOutput(wheel5);
                transformEvent.AddOutput(wheel6);
                transformEvent.AddOutput(wheel7);
                transformEvent.AddOutput(wheel8);
                transformEvent.AddOutput(wheel9);
                transformEvent.AddOutput(wheel10);
                events.Add(transformEvent);
            }

            // ship
            {
                ITEObjectEvent shipEvent2 = new TEObjectEvent();
                shipEvent2.EventTime = DateTime.UtcNow.AddDays(8);
                shipEvent2.EventTimeOffset = 1;
                shipEvent2.Owner = processor1;
                shipEvent2.DataOwner = processor1;
                shipEvent2.Action = TEEventAction.OBSERVE;
                shipEvent2.BusinessStep = "urn:epcglobal:cbv:bizstep:shipping";
                shipEvent2.Disposition = "urn:epcglobal:cbv:disp:in_progress";
                shipEvent2.Location = new TEEventLocation(secondaryProcessingPlant);
                shipEvent2.AddProduct(wheel1);
                shipEvent2.AddProduct(wheel2);
                shipEvent2.AddProduct(wheel3);
                shipEvent2.AddProduct(wheel4);
                shipEvent2.AddProduct(wheel5);
                shipEvent2.AddProduct(wheel6);
                shipEvent2.AddProduct(wheel7);
                shipEvent2.AddProduct(wheel8);
                shipEvent2.AddProduct(wheel9);
                shipEvent2.AddProduct(wheel10);
                shipEvent2.AddBuyer(trainOwner);
                shipEvent2.AddSeller(processor2);
                events.Add(shipEvent2);
            }

            // receive
            {
                ITEObjectEvent receiveEvent3 = new TEObjectEvent();
                receiveEvent3.EventTime = DateTime.UtcNow.AddDays(9);
                receiveEvent3.EventTimeOffset = 1;
                receiveEvent3.Owner = processor1;
                receiveEvent3.DataOwner = processor1;
                receiveEvent3.Action = TEEventAction.OBSERVE;
                receiveEvent3.BusinessStep = "urn:epcglobal:cbv:bizstep:shipping";
                receiveEvent3.Disposition = "urn:epcglobal:cbv:disp:in_progress";
                receiveEvent3.Location = new TEEventLocation(trainYard);
                receiveEvent3.AddProduct(wheel1);
                receiveEvent3.AddProduct(wheel2);
                receiveEvent3.AddProduct(wheel3);
                receiveEvent3.AddProduct(wheel4);
                receiveEvent3.AddProduct(wheel5);
                receiveEvent3.AddProduct(wheel6);
                receiveEvent3.AddProduct(wheel7);
                receiveEvent3.AddProduct(wheel8);
                receiveEvent3.AddProduct(wheel9);
                receiveEvent3.AddProduct(wheel10);
                events.Add(receiveEvent3);
            }

            // associate
            {
                ITEAssociationEvent assEvent = new TEAssociationEvent();
                assEvent.EventTime = DateTime.UtcNow.AddDays(10);
                assEvent.EventTimeOffset = 1;
                assEvent.Owner = processor1;
                assEvent.DataOwner = processor1;
                assEvent.Action = TEEventAction.ADD;
                assEvent.BusinessStep = "urn:epcglobal:cbv:bizstep:shipping";
                assEvent.Disposition = "urn:epcglobal:cbv:disp:in_progress";
                assEvent.Location = new TEEventLocation(trainYard);
                assEvent.ParentID = train;
                assEvent.AddChild(wheel1);
                events.Add(assEvent);
            }

            // disassociate
            {
                ITEAssociationEvent disAssEvent = new TEAssociationEvent();
                disAssEvent.EventTime = DateTime.UtcNow.AddDays(11);
                disAssEvent.EventTimeOffset = 1;
                disAssEvent.Owner = processor1;
                disAssEvent.DataOwner = processor1;
                disAssEvent.Action = TEEventAction.DELETE;
                disAssEvent.BusinessStep = "urn:epcglobal:cbv:bizstep:shipping";
                disAssEvent.Disposition = "urn:epcglobal:cbv:disp:in_progress";
                disAssEvent.Location = new TEEventLocation(trainYard);
                disAssEvent.ParentID = train;
                disAssEvent.AddChild(wheel1);
                events.Add(disAssEvent);
            }

            // dispose
            {
                ITEObjectEvent receiveEvent3 = new TEObjectEvent();
                receiveEvent3.EventTime = DateTime.UtcNow.AddDays(12);
                receiveEvent3.EventTimeOffset = 1;
                receiveEvent3.Owner = processor1;
                receiveEvent3.DataOwner = processor1;
                receiveEvent3.Action = TEEventAction.OBSERVE;
                receiveEvent3.BusinessStep = "urn:epcglobal:cbv:bizstep:shipping";
                receiveEvent3.Disposition = "urn:epcglobal:cbv:disp:in_progress";
                receiveEvent3.Location = new TEEventLocation(trainYard);
                receiveEvent3.AddProduct(wheel1);
                events.Add(receiveEvent3);
            }
        }

        [TestMethod]
        public void JSON_2_0()
        {
            ITEEPCISMapper mapper = new EPCISJsonMapper_2_0();
            foreach (ITEEvent cte in events)
            {
                ITEEPCISDocument docBefore = new TEEPCISDocument();
                docBefore.Events = new List<ITEEvent>() { cte };
                string jsonStr = mapper.WriteEPCISData(docBefore);
                ITEEPCISDocument docAfter = mapper.ReadEPCISData(jsonStr);
                string jsonBefore = JObject.Parse(JsonConvert.SerializeObject(docBefore)).ToString(Formatting.Indented);
                string jsonAfter = JObject.Parse(JsonConvert.SerializeObject(docAfter)).ToString(Formatting.Indented);
                if (Debugger.IsAttached)
                {
                    if (jsonBefore != jsonAfter)
                    {
                        Debugger.Break();
                    }
                }
                Assert.AreEqual(jsonBefore, jsonAfter, "Failed to serialize / deserialize to and from the EPCIS 2.0 JSON format.");
            }
        }

        [TestMethod]
        public void XML_2_0()
        {
            ITEEPCISMapper mapper = new EPCISXmlMapper_2_0();
            foreach (ITEEvent cte in events)
            {
                ITEEPCISDocument docBefore = new TEEPCISDocument();
                docBefore.Events = new List<ITEEvent>() { cte };
                string jsonStr = mapper.WriteEPCISData(docBefore);
                ITEEPCISDocument docAfter = mapper.ReadEPCISData(jsonStr);
                string jsonBefore = JArray.Parse(JsonConvert.SerializeObject(docBefore)).ToString(Formatting.Indented);
                string jsonAfter = JArray.Parse(JsonConvert.SerializeObject(docAfter)).ToString(Formatting.Indented);
                if (Debugger.IsAttached)
                {
                    if (jsonBefore != jsonAfter)
                    {
                        Debugger.Break();
                    }
                }
                Assert.AreEqual(jsonBefore, jsonAfter, "Failed to serialize / deserialize to and from the EPCIS 2.0 JSON format.");
            }
        }

        [TestMethod]
        public void GDST()
        {
            
        }
    }
}
