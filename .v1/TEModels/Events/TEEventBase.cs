using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Interfaces.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Models.Events
{
    public class TEEventBase
    {
        [Key]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ObjectID { get; set; }

        public long ID { get; set; }
        public string EventID { get; set; }
        public DateTimeOffset EventTime { get; set; }
        public double EventTimeOffset { get; set; }
        public IPGLN DataOwner { get; set; }
        public IPGLN Owner { get; set; }
        public TEEventAction Action { get; set; }
        public string BusinessStep { get; set; }
        public string Disposition { get; set; }
        public ITEPersistentDisposition PersistentDisposition { get; set; }
        public ITEEventLocation Location { get; set; }
        public ITEEventReadPoint ReadPoint { get; set; }
        public List<ITEEventSource> SourceList { get; set; } = new List<ITEEventSource>();
        public List<ITEEventDestination> DestinationList { get; set; } = new List<ITEEventDestination>();
        public List<ITECertificate> Certificates { get; set; } = new List<ITECertificate>();
        public List<ITEEventKDE> KDEs { get; set; } = new List<ITEEventKDE>();
        public List<ITEAttachment> Attachments { get; set; } = new List<ITEAttachment>();
        public List<ITEEventBusinessTransaction> BusinessTransactions { get; set; } = new List<ITEEventBusinessTransaction>();
        public List<ITESensorElement> SensorElementList { get; set; } = new List<ITESensorElement>();
        public ITEErrorDeclaration ErrorDeclaration { get; set; }

        public T GetKDE<T>(string name, string ns = null)
        {
            ITEEventKDE kde = KDEs.Find(k => k.Name == name && (ns == null || k.Namespace == ns || k.NamespacePrefix == ns));
            if (kde != null)
            {
                if (kde is T)
                {
                    return (T)kde;
                }
            }
            return default(T);
        }

        public void AddSeller(IPGLN pgln)
        {
            if (!this.SourceList.Exists(s => s.Type == TEEventSourceType.Owner && s.Value == pgln.ToString()))
            {
                this.SourceList.Add(new TEEventSource(pgln, TEEventSourceType.Owner));
            }
        }

        public void AddBuyer(IPGLN pgln)
        {
            if (!this.DestinationList.Exists(s => s.Type == TEEventDestinationType.Owner && s.Value == pgln.ToString()))
            {
                this.DestinationList.Add(new TEEventDestination(pgln, TEEventDestinationType.Owner));
            }
        }
    }
}
