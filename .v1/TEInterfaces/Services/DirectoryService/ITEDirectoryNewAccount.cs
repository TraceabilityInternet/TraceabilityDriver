using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Interfaces.Services.DirectoryService
{
    public interface ITEDirectoryNewAccount : ITEDirectoryAccount
    {
        ISimpleSignature AccountSignature { get; set; }
        ISimpleSignature ServiceProviderSignature { get; set; }
        IDID ServiceProviderDID { get; set; }
        IDID DID { get; set; }
        bool VerifySignature();
        void Sign();
        ITEDirectoryAccount ToAccount();
    }
}
