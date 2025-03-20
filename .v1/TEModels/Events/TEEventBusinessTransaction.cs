using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;

namespace TraceabilityEngine.Models.Events
{
    public class TEEventBusinessTransaction : ITEEventBusinessTransaction
    {
        public string RawType { get; set; }
        public EventBusinessTransactionType Type { get; set; }
        public string Value { get; set; }
    }
}
