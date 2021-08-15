using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Interfaces.Driver
{
    public enum TradingPartnerCommunicationProtocol
    {
        Classic = 0,
        EPCISQueryInterfaceOnly = 1
    }

    public interface ITEDriverTradingPartner : ITEDocumentObject
    {
        long ID { get; set; }
        long AccountID { get; set; }
        string Name { get; set; }
        IPGLN PGLN { get; set; }
        string DigitalLinkURL { get; set; }
        IDID DID { get; set; }
        TradingPartnerCommunicationProtocol Protocol { get; set; }

        string ToJson();
        void FromJson(string json);
    }
}
