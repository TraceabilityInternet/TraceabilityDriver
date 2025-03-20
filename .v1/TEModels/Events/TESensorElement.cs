using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;

namespace TraceabilityEngine.Models.Events
{
    public class TESensorElement : ITESensorElement
    {
        public DateTime? TimeStamp { get; set; }
        public Uri DeviceID { get; set; }
        public string DeviceMetaData { get; set; }
        public Uri RawData { get; set; }
        public List<ITESensorReport> Reports { get; set; }
    }
}
