using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Models.DB.Sql
{
    /// <summary>
    /// An EPCIS event row: the full event JSON identified by its EPCIS event id.
    /// </summary>
    /// <remarks>
    /// Synced events carry the deployment version they were synced under and the event key they are
    /// upserted by. Traceback rows carry a null deployment version and no event key, and are never
    /// updated once stored.
    /// </remarks>
    public class EPCISEventSqlDocument
    {
        public long ID { get; set; } = 0;

        /// <summary>
        /// The EPCIS event id, generated from the event content with the EventHashGenerator when the
        /// event is saved (traceback events keep the id they arrived with).
        /// </summary>
        public string EventId { get; set; } = string.Empty;

        /// <summary>
        /// The deterministic source-event key synced events are upserted by. Null on traceback rows.
        /// </summary>
        public string? EventKey { get; set; } = null;

        /// <summary>
        /// The deployment version the event was synced under. Null on traceback rows.
        /// </summary>
        public string? DeploymentVersion { get; set; } = null;

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
