using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Interfaces.Driver
{
    public interface ITEDriverAccount : ITEDocumentObject
    {
        long ID { get; set; }
        string Name { get; set; }
        IPGLN PGLN { get; set; }
        IDID DID { get; set; }
        string DigitalLinkURL { get; set; }
        ITEDirectoryNewAccount ToDirectoryAccount(IDID serviceProviderDID, IPGLN serviceProviderPGLN);
        string ToJson();
        void FromJson(string jsonStr);
    }
}
