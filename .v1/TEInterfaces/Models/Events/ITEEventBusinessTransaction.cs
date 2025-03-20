using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public enum EventBusinessTransactionType
    {

    }

    public interface ITEEventBusinessTransaction
    {
        string RawType { get; set; }
        EventBusinessTransactionType Type { get; }
        string Value { get; set; }
    }
}
