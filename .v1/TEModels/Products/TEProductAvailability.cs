using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductAvailability : ITEProductAvailability
    {
        public TEProductAvailability()
        {

        }

        public double Price { get; set; }

        public IGLN GLN { get; set; }
    }
}
