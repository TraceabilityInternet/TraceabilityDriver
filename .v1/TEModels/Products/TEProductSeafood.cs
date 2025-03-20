using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.StaticData;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductSeafood : ITEProductSeafood
    {
        public ProductionMethod ProductionMethod { get; set; }
        public ProductCondition ProductCondition { get; set; }
        public ProductForm ProductForm { get; set; }
        public RearingMethod RearingMethod { get; set; }
        public FishingMethod FishingMethod { get; set; }
    }
}
