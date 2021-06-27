using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Interfaces.Models.Common
{
    public interface ITEAnonymizedAddress
    {
        string ZipCode { get; set; }
        Country Country { get; set; }
    }
}
