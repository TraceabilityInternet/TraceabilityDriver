using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Locations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Locations
{
    public class TEVesselInformation : ITEVesselInformation
    {
        public string IMONumber { get; set; }
        public string RMFONumber { get; set; }
        public string VesselCallSign { get; set; }
        public Country VesselFlag { get; set; }
    }
}
