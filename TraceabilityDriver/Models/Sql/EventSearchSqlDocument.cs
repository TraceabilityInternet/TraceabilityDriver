using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace TraceabilityDriver.Models.Sql
{
    /// <summary>
    /// A model class for entity framework for saving searchable fields into the SQL server
    /// in a way that they can also be indexed.
    /// </summary>
    public class EventSearchSqlDocument
    {
        [Key]
        public long ID { get; set; }

        /// <summary>
        /// The event ID that is being indexed.
        /// </summary>
        public string EventId { get; set; } = string.Empty;

        /// <summary>
        /// Represents the business step associated with a process. Initialized to an empty string.
        /// </summary>
        public string BizStep { get; set; } = string.Empty;

        /// <summary>
        /// Represents an action as a string. Initialized to an empty string by default.
        /// </summary>
        public string Action { get; set; } = string.Empty;

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

        public static List<EventSearchSqlDocument> CreateSearchDocuments(List<IEvent> evts)
        {
            List<EventSearchSqlDocument> searchDocument = new List<EventSearchSqlDocument>();

            foreach (IEvent evt in evts)
            {
                string bizStep = evt.BusinessStep.ToString().ToLower();
                string action = evt.Action.ToString()?.ToLower() ?? "";
                DateTimeOffset? eventTime = evt.EventTime;
                List<string> epcs = evt.Products.Select(p => p.EPC.ToString().ToLower()).ToList();
                List<string> productGTINs = evt.Products.Select(p => p.EPC.GTIN?.ToString().ToLower()).Where(g => g != null).Select(g => g!).ToList();
                List<string> locationGLNs = evt.Location?.GLN != null ? new List<string> { evt.Location.GLN.ToString().ToLower() } : new List<string>();
                List<string> partyPGLNs = new List<string>();

                // Add trading party PGLNs if it's a GDST event
                if (evt is IGDSTEvent gdstEvent)
                {
                    if (gdstEvent.ProductOwner != null)
                    {
                        partyPGLNs.Add(gdstEvent.ProductOwner.ToString());
                    }
                    if (gdstEvent.InformationProvider != null)
                    {
                        partyPGLNs.Add(gdstEvent.InformationProvider.ToString());
                    }
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
                        EventTime = eventTime,
                        EPC = i < epcs.Count ? epcs[i] : string.Empty,
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
