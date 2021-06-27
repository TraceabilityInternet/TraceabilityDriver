using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Models.Events
{
    public class TEEventLocation : ITEEventLocation
    {
        public TEEventLocation()
        {

        }

        public TEEventLocation(IGLN gln)
        {
            this.GLN = gln;
        }

        public IGLN GLN { get; set; }
    }
}
