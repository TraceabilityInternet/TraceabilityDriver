using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Models.Locations;

namespace TraceabilityEngine.Models.Locations
{
    public class TELocationLite : ITELocationLite
    {
        public long ID { get; set; }
        public bool IsPublic { get; set; }
        public bool Archived { get; set; }
        public IGLN GLN { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
