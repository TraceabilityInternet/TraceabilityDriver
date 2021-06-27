using TraceabilityEngine.Interfaces.Models.Locations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Locations
{
    public class TEFarmInformation : ITEFarmInformation
    {
        public string PermitNumber { get; set; }
        public string FarmNumber { get; set; }
    }
}
