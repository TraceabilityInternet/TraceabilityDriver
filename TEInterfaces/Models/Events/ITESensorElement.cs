using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public interface ITESensorElement
    {
        DateTime? TimeStamp { get; set; }
        Uri DeviceID { get; set; }
        string DeviceMetaData { get; set; }
        Uri RawData { get; set; }
        List<ITESensorReport> Reports { get; set; }
    }
}
