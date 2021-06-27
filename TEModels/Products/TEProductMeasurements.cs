using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductMeasurements : ITEProductMeasurements
    {
        public TEMeasurement NetWeight { get; set; }
        public TEMeasurement DrainedWeight { get; set; }
        public TEMeasurement GrossWeight { get; set; }
        public TEMeasurement NetContent { get; set; }
        public TEMeasurement PackagedHeight { get; set; }
        public TEMeasurement PackagedWidth { get; set; }
        public TEMeasurement PackagedLength { get; set; } 
        public TEMeasurement PackagedDiameter { get; set; }
        public TEMeasurement OutOfPackagedHeight { get; set; }
        public TEMeasurement OutOfPackagedWidth { get; set; }
        public TEMeasurement OutOfPackagedLength { get; set; }
        public TEMeasurement OutOfPackagedDiameter { get; set; }
        public string SizeDescription { get; set; }
        public string SizeCodeListCode { get; set; }
        public string SizeCodeValue { get; set; }
        public bool IsVariableWeight { get; set; }
        public string PackMediumKey { get; set; }
    }
}
