using TraceabilityEngine.Util.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductMeasurements
    {
        TEMeasurement NetWeight { get; set; }
        TEMeasurement DrainedWeight { get; set; }
        TEMeasurement GrossWeight { get; set; }
        TEMeasurement NetContent { get; set; }
        TEMeasurement PackagedHeight { get; set; }
        TEMeasurement PackagedWidth { get; set; }
        TEMeasurement PackagedLength { get; set; }
        TEMeasurement PackagedDiameter { get; set; }
        TEMeasurement OutOfPackagedHeight { get; set; }
        TEMeasurement OutOfPackagedWidth { get; set; }
        TEMeasurement OutOfPackagedLength { get; set; }
        TEMeasurement OutOfPackagedDiameter { get; set; }
        string SizeDescription { get; set; }
        string SizeCodeListCode { get; set; }
        string SizeCodeValue { get; set; }
        bool IsVariableWeight { get; set; }
        string PackMediumKey { get; set; }
    }
}
