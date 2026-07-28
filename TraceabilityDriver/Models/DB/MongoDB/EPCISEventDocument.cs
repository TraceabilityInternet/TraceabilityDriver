using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Models.DB.MongoDB
{
    public class EPCISEventDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string EventId { get; set; } = string.Empty;

        /// <summary>
        /// The deterministic source-event key synced events are upserted by. Null on traceback documents.
        /// </summary>
        public string? EventKey { get; set; }

        /// <summary>
        /// The deployment version the event was synced under. Null on traceback documents, which are
        /// not versioned.
        /// </summary>
        public string? DeploymentVersion { get; set; }

        /// <summary>
        /// The merged common event the synced event was converted from, so later sync runs can merge
        /// additional source rows into it. Null on traceback documents.
        /// </summary>
        public string? CommonEventJson { get; set; }

        public string EventJson { get; set; } = string.Empty;

        public string BizStep { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public DateTimeOffset? EventTime { get; set; }

        public DateTime RecordTime { get; set; } = DateTime.UtcNow;

        public DateTime AuditTime { get; set; } = DateTime.UtcNow;

        public List<string> EPCs { get; set; } = new List<string>();

        public List<string> ProductGTINs { get; set; } = new List<string>();

        public List<string> LocationGLNs { get; set; } = new List<string>();

        public List<string> PartyPGLNs { get; set; } = new List<string>();

        public EPCISEventDocument()
        {
        }

        public EPCISEventDocument(IEvent evt)
        {
            EPCISQueryDocument doc = new EPCISQueryDocument();
            doc.EPCISVersion = EPCISVersion.V2;
            doc.Header = OpenTraceability.Models.Common.StandardBusinessDocumentHeader.DummyHeader;
            doc.Events.Add(evt);
            string json = OpenTraceability.Mappers.OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(doc);

            EventId = evt.EventID.ToString();
            EventJson = json;
            BizStep = evt.BusinessStep.ToString().ToLower();
            Action = evt.Action.ToString()?.ToLower() ?? "";
            EventTime = evt.EventTime;
            EPCs = evt.Products.Select(p => p.EPC.ToString().ToLower()).ToList();
            ProductGTINs = evt.Products.Select(p => p.EPC.GTIN?.ToString().ToLower()).Where(g => g != null).Select(g => g!).ToList();
            LocationGLNs = evt.Location?.GLN != null ? new List<string> { evt.Location.GLN.ToString().ToLower() } : new List<string>();
            PartyPGLNs = new List<string>();

            // Add trading party PGLNs if it's a GDST event. The product owner only exists on
            // events implementing IGDSTProductOwnerEvent in the GDST 2.0 model.
            if (evt is IGDSTProductOwnerEvent productOwnerEvent && productOwnerEvent.ProductOwner != null)
            {
                PartyPGLNs.Add(productOwnerEvent.ProductOwner.ToString());
            }
            if (evt is IGDSTEvent gdstEvent && gdstEvent.InformationProvider != null)
            {
                PartyPGLNs.Add(gdstEvent.InformationProvider.ToString());
            }

            // Add source and destination PGLNs/GLNs
            foreach (var source in evt.SourceList)
            {
                if (!string.IsNullOrWhiteSpace(source.Value))
                {
                    if (source.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Possessor ||
                        source.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Owner)
                    {
                        PartyPGLNs.Add(source.Value);
                    }
                    else if (source.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Location)
                    {
                        LocationGLNs.Add(source.Value);
                    }
                }
            }

            foreach (var dest in evt.DestinationList)
            {
                if (!string.IsNullOrWhiteSpace(dest.Value))
                {
                    if (dest.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Possessor ||
                        dest.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Owner)
                    {
                        PartyPGLNs.Add(dest.Value);
                    }
                    else if (dest.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Location)
                    {
                        LocationGLNs.Add(dest.Value);
                    }
                }
            }
        }
    }
}