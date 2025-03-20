using TraceabilityEngine.Interfaces.Models.Common;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Interfaces.Models.Account
{
    public interface ITEAccount : ITEDocumentObject
    {
        long ID { get; set; }
        string Name { get; set; }
        IPGLN PGLN { get; set; }
        IDID DID { get; set; }
        ITEAnonymizedAddress AnonymizedAddress { get; set; }
        Dictionary<string, string> MetaData { get; set; }
    }
}
