using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductColour : ITEProductColour
    {
        public ColourCode ColourCode { get; set; }
        public string Value { get; set; }
    }
}
