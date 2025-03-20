using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductPreparation
    {
        string PreparationInstructions { get; set; }
        string PreparationConsumptionPrecautions { get; set; }
        PreparationTypeCode ManufacturerPreparationTypeCode { get; set; }
        PreparationTypeCode PreparationTypeCode { get; set; }
        TEMeasurement OptimumConsumptionMinTemperature { get; set; }
        TEMeasurement OptimumConsumptionMaxTemperature { get; set; }
        double ConvenienceLevelPercent { get; set; }
        List<ITEProductYield> ProductYields { get; set; }
    }
}
