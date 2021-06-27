using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductChild : ITEProductChild
    {
        public IGTIN ChildGTIN { get; set; }
        public int InnerProductCount { get; set; }
    }
}
