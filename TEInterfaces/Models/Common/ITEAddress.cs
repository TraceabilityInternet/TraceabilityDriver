using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Common
{
    public interface ITEAddress : ITEAnonymizedAddress
    {
        string Address1 { get; set; }
        string Address2 { get; set; }
        string City { get; set; }
        string State { get; set; }
        string County { get; set; }
        string ToString();
    }
}
