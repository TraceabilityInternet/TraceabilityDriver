using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductYield
    {
        ProductYieldTypeCode ProductYieldType { get; set; }
        TEMeasurement Amount { get; set; }
        double VariationPercent { get; set; }
    }
}
