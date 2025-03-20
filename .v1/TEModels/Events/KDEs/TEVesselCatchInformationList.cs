using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Models.Events.KDEs
{
    [TEKey("cbvmda:vesselCatchInformationList")]
    public class TEVesselCatchInformationList : ITEEventKDE
    {
        public string Namespace => "cbvmda";
        public string NamespacePrefix => "cbvmda";
        public string Name => "vesselCatchInformationList";
        public XElement XmlValue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public JToken JsonValue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Type ValueType => typeof(TEVesselCatchInformationList);
        public List<TEVesselCatchInformation> List { get; set; } = new List<TEVesselCatchInformation>();
    }
}
