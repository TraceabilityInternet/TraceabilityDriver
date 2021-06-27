using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductAvailability
    { 
        double Price { get; set; }

        IGLN GLN { get; set; }
    }
}
