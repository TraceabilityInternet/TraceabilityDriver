using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductTreatment : ITEProductTreatment
    {
        public TreatmentType TreatmentType { get; set; }
        public TEMeasurement Time { get; set; }
        public TEMeasurement Temperature { get; set; }
        public TEMeasurement Concentration { get; set; }
    }
}
