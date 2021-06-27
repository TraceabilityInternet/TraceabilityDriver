using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductPreparation : ITEProductPreparation
    {
        public string PreparationInstructions { get; set; }
        public string PreparationConsumptionPrecautions { get; set; }
        public PreparationTypeCode ManufacturerPreparationTypeCode { get; set; }
        public PreparationTypeCode PreparationTypeCode { get; set; }
        public TEMeasurement OptimumConsumptionMinTemperature { get; set; }
        public TEMeasurement OptimumConsumptionMaxTemperature { get; set; }
        public double ConvenienceLevelPercent { get; set; }
        public List<ITEProductYield> ProductYields { get; set; } = new List<ITEProductYield>();
    }
}
