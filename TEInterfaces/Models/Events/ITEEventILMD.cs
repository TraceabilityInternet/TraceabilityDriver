using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public interface ITEEventILMD
    {
        string LotNumber { get; set; }
        DateTime? ExpirationDate { get; set; }
        List<ITEEventKDE> KDEs { get; set; }
        T GetKDE<T>(string name, string ns = null);
    }
}
