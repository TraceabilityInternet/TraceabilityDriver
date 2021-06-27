using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.DB.DocumentDB;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public enum TEEventType
    {
        Object,
        Transformation,
        Aggregation,
        Transaction,
        Association
    }

    public enum TEEventAction
    {
        ADD,
        OBSERVE,
        DELETE
    }

    public interface ITEEvent : ITEDocumentObject
    {
        long ID { get; set; }
        string EventID { get; set; }
        IPGLN DataOwner { get; set; }
        IPGLN Owner { get; set; }
        DateTimeOffset EventTime { get; set; }
        double EventTimeOffset { get; set; }
        TEEventType EventType { get; }
        TEEventAction Action { get; set; }
        string BusinessStep { get; set; }
        string Disposition { get; set; }
        ITEPersistentDisposition PersistentDisposition { get; set; }
        ITEErrorDeclaration ErrorDeclaration { get; set; }
        ITEEventLocation Location { get; set; }
        ITEEventReadPoint ReadPoint { get; set; }
        List<ITEEventBusinessTransaction> BusinessTransactions { get; set; }
        List<ITEEventSource> SourceList { get; set; }
        List<ITEEventDestination> DestinationList { get; set; }
        List<ITECertificate> Certificates { get; set; }
        List<ITEAttachment> Attachments { get; set; }
        List<ITEEventKDE> KDEs { get; set; }
        List<ITESensorElement> SensorElementList { get; set; }
        ITEEventILMD ILMD { get; set; }
        List<ITEEventProduct> Products { get; }
        T GetKDE<T>(string name, string ns = null);

        void AddBuyer(IPGLN pgln);
        void AddSeller(IPGLN pgln);
    }
}
