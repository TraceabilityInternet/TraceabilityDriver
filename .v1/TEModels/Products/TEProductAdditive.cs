using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductAdditive : ITEProductAdditive
    {
        public LevelOfContainmentCode LevelOfContainment { get; set; }
        public string Name { get; set; }
    }
}
