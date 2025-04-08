using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Models.Sql
{
    public class EPCISEventSqlDocument
    {
        public long ID { get; set; } = 0;

        public string EventId { get; set; } = string.Empty;

        public string EventJson { get; set; } = string.Empty;

        public string BizStep { get; set; } = string.Empty;

        public EPCISEventSqlDocument()
        {
        }

        public EPCISEventSqlDocument(IEvent evt)
        {
            EPCISQueryDocument doc = new EPCISQueryDocument();
            doc.EPCISVersion = EPCISVersion.V2;
            doc.Header = OpenTraceability.Models.Common.StandardBusinessDocumentHeader.DummyHeader;
            doc.Events.Add(evt);
            string json = OpenTraceability.Mappers.OpenTraceabilityMappers.EPCISQueryDocument.JSON.Map(doc);

            EventId = evt.EventID.ToString();
            EventJson = json;
            BizStep = evt.BusinessStep.ToString().ToLower();
        }
    }
}