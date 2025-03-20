using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.StaticData;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductSeafood
    {
        ProductionMethod ProductionMethod { get; set; }
        ProductCondition ProductCondition { get; set; }
        ProductForm ProductForm { get; set; }
        RearingMethod RearingMethod { get; set; }
        FishingMethod FishingMethod { get; set; }
    }
}
