using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Common
{
    public interface ITEContact
    {
        string ContactType { get; set; }
        string ContactTitle { get; set; }
        string Responsibility { get; set; }
        string Name { get; set; }
        string Email { get; set; }
        string Phone { get; set; }
        string Fax { get; set; }
        List<TELanguage> Languages { get; set; }
    }
}
