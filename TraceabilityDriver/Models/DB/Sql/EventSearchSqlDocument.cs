using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using System.ComponentModel.DataAnnotations;

namespace TraceabilityDriver.Models.DB.Sql
{
    /// <summary>
    /// A flattened, indexable event search row used to filter events in SQL server.
    /// </summary>
    /// <remarks>
    /// One row is created per "slot" of an event's searchable values (EPCs, GTINs, GLNs, PGLNs); unused
    /// columns on a row are left empty. Synced rows carry the deployment version and event key of the
    /// event that produced them so a resync can replace them; traceback rows carry neither.
    /// </remarks>
    public class EventSearchSqlDocument
    {
        [Key]
        public long ID { get; set; }

        /// <summary>
        /// The event ID that is being indexed.
        /// </summary>
        public string EventId { get; set; } = string.Empty;

        /// <summary>
        /// The event key of the synced event the row belongs to, used to replace the rows when the
        /// event is stored again. Null on traceback rows.
        /// </summary>
        public string? EventKey { get; set; } = null;

        /// <summary>
        /// The deployment version the event was synced under. Null on traceback rows.
        /// </summary>
        public string? DeploymentVersion { get; set; } = null;

        /// <summary>
        /// Represents the business step associated with a process. Initialized to an empty string.
        /// </summary>
        public string BizStep { get; set; } = string.Empty;

        /// <summary>
        /// Represents an action as a string. Initialized to an empty string by default.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// The lowercased EPCIS event type (e.g. "objectevent"), matched by the eventTypes query parameter.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// The lowercased transformation id, matched by the EQ_transformationID query parameter.
        /// Empty on every event that is not a transformation event.
        /// </summary>
        public string TransformationId { get; set; } = string.Empty;

        /// <summary>
        /// Represents the time when an event occurred, stored as a nullable DateTimeOffset. If no event time is set, it
        /// can be null.
        /// </summary>
        public DateTimeOffset? EventTime { get; set; }

        /// <summary>
        /// Represents the time when a record was created, initialized to the current UTC time.
        /// </summary>
        public DateTime RecordTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// EPC is a string property that holds a value related to an Electronic Product Code. It is initialized to an
        /// empty string.
        /// </summary>
        public string EPC { get; set; } = string.Empty;

        /// <summary>
        /// Whether <see cref="EPC"/> came from a reference or child product. The MATCH_epc and
        /// MATCH_epcClass query parameters only match reference and child EPCs, while MATCH_anyEPC and
        /// MATCH_anyEPCClass match every product EPC.
        /// </summary>
        public bool EPCIsReferenceOrChild { get; set; }

        /// <summary>
        /// Represents the Global Trade Item Number (GTIN) of a product. Initialized to an empty string by default.
        /// </summary>
        public string ProductGTIN { get; set; } = string.Empty;

        /// <summary>
        /// Represents the Global Location Number (GLN) for a location. Initialized to an empty string.
        /// </summary>
        public string LocationGLN { get; set; } = string.Empty;

        /// <summary>
        /// Represents the Party PGLN as a string. Initialized to an empty string by default.
        /// </summary>
        public string PartyPGLN { get; set; } = string.Empty;

        /// <summary>
        /// Creates the flattened search rows for the given events, one row per searchable value slot.
        /// </summary>
        /// <param name="evts">The events to flatten into search rows.</param>
        /// <returns>The search rows for all of the given events.</returns>
        public static List<EventSearchSqlDocument> CreateSearchDocuments(List<IEvent> evts)
        {
            List<EventSearchSqlDocument> searchDocument = new List<EventSearchSqlDocument>();

            foreach (IEvent evt in evts)
            {
                string bizStep = evt.BusinessStep.ToString().ToLower();
                string action = evt.Action.ToString()?.ToLower() ?? "";
                string eventType = evt.EventType.ToString().ToLower();
                DateTimeOffset? eventTime = evt.EventTime;

                // The record time was stamped by the store right before the rows are built; copying it
                // keeps the queryable rows in exact agreement with the record time serialized into the
                // stored event JSON.
                DateTime recordTime = evt.RecordTime?.UtcDateTime ?? DateTime.UtcNow;

                // Only transformation events carry a transformation id.
                string transformationId = evt is ITransformationEvent transformationEvent && !string.IsNullOrWhiteSpace(transformationEvent.TransformationID) ? transformationEvent.TransformationID.ToLower() : string.Empty;

                // Each EPC keeps whether it came from a reference or child product, because the
                // MATCH_epc/MATCH_epcClass parameters only match those product types.
                List<(string Value, bool IsReferenceOrChild)> epcs = evt.Products.Select(p => (p.EPC.ToString().ToLower(), p.Type == EventProductType.Reference || p.Type == EventProductType.Child)).ToList();
                List<string> productGTINs = evt.Products.Select(p => p.EPC.GTIN?.ToString().ToLower()).Where(g => g != null).Select(g => g!).ToList();
                List<string> locationGLNs = evt.Location?.GLN != null ? new List<string> { evt.Location.GLN.ToString().ToLower() } : new List<string>();
                List<string> partyPGLNs = new List<string>();

                // Add trading party PGLNs if it's a GDST event. The product owner only exists on
                // events implementing IGDSTProductOwnerEvent in the GDST 2.0 model.
                if (evt is IGDSTProductOwnerEvent productOwnerEvent && productOwnerEvent.ProductOwner != null)
                {
                    partyPGLNs.Add(productOwnerEvent.ProductOwner.ToString());
                }
                if (evt is IGDSTEvent gdstEvent && gdstEvent.InformationProvider != null)
                {
                    partyPGLNs.Add(gdstEvent.InformationProvider.ToString());
                }

                // Add source and destination PGLNs/GLNs
                foreach (var source in evt.SourceList)
                {
                    if (!string.IsNullOrWhiteSpace(source.Value))
                    {
                        if (source.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Possessor ||
                            source.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Owner)
                        {
                            partyPGLNs.Add(source.Value);
                        }
                        else if (source.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Location)
                        {
                            locationGLNs.Add(source.Value);
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
                            partyPGLNs.Add(dest.Value);
                        }
                        else if (dest.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Location)
                        {
                            locationGLNs.Add(dest.Value);
                        }
                    }
                }

                // Get the higest index to iterate through
                int maxCount = Math.Max(Math.Max(Math.Max(epcs.Count, productGTINs.Count), locationGLNs.Count), partyPGLNs.Count);

                for (int i = 0; i < maxCount; i++)
                {
                    EventSearchSqlDocument searchDoc = new EventSearchSqlDocument
                    {
                        EventId = evt.EventID.ToString(),
                        BizStep = bizStep,
                        Action = action,
                        EventType = eventType,
                        TransformationId = transformationId,
                        EventTime = eventTime,
                        RecordTime = recordTime,
                        EPC = i < epcs.Count ? epcs[i].Value : string.Empty,
                        EPCIsReferenceOrChild = i < epcs.Count && epcs[i].IsReferenceOrChild,
                        ProductGTIN = i < productGTINs.Count ? productGTINs[i] : string.Empty,
                        LocationGLN = i < locationGLNs.Count ? locationGLNs[i] : string.Empty,
                        PartyPGLN = i < partyPGLNs.Count ? partyPGLNs[i] : string.Empty
                    };
                    searchDocument.Add(searchDoc);
                }
            }

            return searchDocument;
        }
    }
}
