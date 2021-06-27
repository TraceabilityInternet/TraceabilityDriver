using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models
{
    public class TEContact : ITEContact
    {
        public string ContactType { get; set; }
        public string ContactTitle { get; set; }
        public string Responsibility { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public List<TELanguage> Languages { get; set; }
    }
}
