using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductYield : ITEProductYield
    {
        public ProductYieldTypeCode ProductYieldType { get; set; }
        public TEMeasurement Amount { get; set; }
        public double VariationPercent { get; set; }
    }
}
