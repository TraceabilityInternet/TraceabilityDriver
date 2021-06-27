using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public enum TEEventDestinationType
    {
        Unknown = 0,

        [TEKey("urn:epcglobal:cbv:sdt:owning_party")]
        Owner = 1,

        [TEKey("urn:epcglobal:cbv:sdt:possessing_party")]
        Possessor = 2,

        [TEKey("urn:epcglobal:cbv:sdt:location")]
        Location = 3
    }

    public interface ITEEventDestination
    {
        string RawType { get; set; }
        TEEventDestinationType Type { get; }
        string Value { get; set; }
    }
}
