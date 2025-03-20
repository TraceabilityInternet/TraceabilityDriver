using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models
{
    public class TECertificate : ITECertificate
    {
        public CertificateType CertificateType { get; set; }
        public ITEAttachment Attachment { get; set; }
        public string Agency { get; set; }
        public string Standard { get; set; }
        public string Value { get; set; }
        public string Identification { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Uri URI { get; set; }
        public Uri AgencyURL { get; set; }
        public DateTime? AuditDate { get; set; }
        public string Subject { get; set; }
    }
}
