using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Interfaces.Models.TradingParty
{
    public interface ITETradingParty
    {
        string Name { get; set; }
        string Department { get; set; }
        IPGLN PGLN { get; set; }
        Uri OrganizationWebURI { get; set; }
        string AdditionalOrganizationType { get; set; }
        OrganizationRole OrganizationRole { get; set; }
        ITEAddress Address { get; set; }
        List<ITEPhoto> Logo { get; set; }
        List<ITEContact> Contacts { get; set; }
        List<ITECertificate> Certificates { get; set; }
    }
}
