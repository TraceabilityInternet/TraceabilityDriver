using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Interfaces.Models.Locations
{
    public interface ITEVesselInformation
    {
        string IMONumber { get; set; }
        string RMFONumber { get; set; }
        string VesselCallSign { get; set; }
        Country VesselFlag { get; set; }
    }
}
