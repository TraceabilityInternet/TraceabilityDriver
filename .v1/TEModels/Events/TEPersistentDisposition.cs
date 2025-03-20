using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;

namespace TraceabilityEngine.Models.Events
{
    public class TEPersistentDisposition : ITEPersistentDisposition
    {
        public List<string> Set { get; set; }
        public List<string> Unset { get; set; }
    }
}
