using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.DB.DocumentDB;

namespace TraceabilityEngine.Interfaces.Models.DigitalLink
{
    public interface ITEDigitalLink : ITEDocumentObject
    {
        string link { get; set; }
        string title { get; set; }
        string linkType { get; set; }
        string ianaLanguage { get; set; }
        string context { get; set; }
        string mimeType { get; set; }
        bool active { get; set; }
        bool fwqs { get; set; }
        bool defaultLinkType { get; set; }
        bool defaultIanaLanguage { get; set; }
        bool defaultContext { get; set; }

        bool defaultMimeType { get; set; }

        string identifier { get; set; }
        bool authRequired { get; set; }
    }
}
