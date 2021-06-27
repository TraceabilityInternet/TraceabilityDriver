using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Locations
{
    public interface ITEFarmInformation
    {
        string PermitNumber { get; set; }
        string FarmNumber { get; set; }
    }
}
