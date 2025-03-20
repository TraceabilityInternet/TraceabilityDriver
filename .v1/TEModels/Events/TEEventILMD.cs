using TraceabilityEngine.Interfaces.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Models.Events
{
    public class TEILMD : ITEEventILMD
    {
        public string LotNumber { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public List<ITEEventKDE> KDEs { get; set; }

        public T GetKDE<T>(string name, string ns = null)
        {
            ITEEventKDE kde = KDEs.Find(k => k.Name == name && (ns == null || k.Namespace == ns || k.NamespacePrefix == ns));
            if (kde != null)
            {
                if (kde is T)
                {
                    return (T)kde;
                }
            }
            return default(T);
        }
    }
}
