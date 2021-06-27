using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public enum NutrientBaseQuantityType
    {
        [TEDisplayName("By Measure")]
        ByMeasure = 0,

        [TEDisplayName("By Serving")]
        ByServing = 1
    }

    public enum GS1MeasurementPrecision
    {
        [TEDisplayName("Approximately")]
        Approximately = 0,

        [TEDisplayName("Exactly")]
        Exactly = 1
    }
}
