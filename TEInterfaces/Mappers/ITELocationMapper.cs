using TraceabilityEngine.Interfaces.Models.Locations;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Mappers
{
    public interface ITELocationMapper
    {
        ITELocation ConvertToLocation(string json);
        string ConvertFromLocation(ITELocation location);
    }
}
