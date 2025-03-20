using TraceabilityEngine.Interfaces.Models.Identifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public enum TEEventReadPointType
    {
        Unknown = 0,
        GLN = 1
    }

    public interface ITEEventReadPoint
    {
        public string ID { get; set; }
    }
}
