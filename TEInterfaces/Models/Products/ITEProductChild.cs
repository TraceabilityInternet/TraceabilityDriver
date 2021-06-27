using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductChild
    {
        IGTIN ChildGTIN { get; set; }
        int InnerProductCount { get; set; }
    }
}
