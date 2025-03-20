using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Models.TradingParty
{
    public class TETradingParty : ITETradingParty
    {
        public string Name { get; set; }
        public string Department { get; set; }
        public IPGLN PGLN { get; set; }
        public Uri OrganizationWebURI { get; set; }
        public string AdditionalOrganizationType { get; set; }
        public OrganizationRole OrganizationRole { get; set; }
        public ITEAddress Address { get; set; } = new TEAddress();
        public List<ITEPhoto> Logo { get; set; } = new List<ITEPhoto>();
        public List<ITEContact> Contacts { get; set; } = new List<ITEContact>();
        public List<ITECertificate> Certificates { get; set; } = new List<ITECertificate>();
    }
}
