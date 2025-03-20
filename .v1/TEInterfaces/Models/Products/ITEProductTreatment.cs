using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductTreatment
    {
        TreatmentType TreatmentType { get; set; }
        TEMeasurement Time { get; set; }
        TEMeasurement Temperature { get; set; }
        TEMeasurement Concentration { get; set; }
    }
}
