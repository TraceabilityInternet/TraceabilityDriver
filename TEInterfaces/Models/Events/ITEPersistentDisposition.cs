using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public interface ITEPersistentDisposition
    {
        List<string> Set { get; set; }
        List<string> Unset { get; set; }
    }
}
