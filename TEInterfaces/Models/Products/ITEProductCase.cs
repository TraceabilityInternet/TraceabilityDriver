using TraceabilityEngine.Util.StaticData;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductCase
    {
        IGTIN CaseGTIN { get; set; }
        string CaseName { get; set; }
        TEMeasurement Height { get; set; }
        TEMeasurement Width { get; set; }
        TEMeasurement Length { get; set; }
        int InnerProductCount { get; set; }
        TEMeasurement NetWeight { get; set; }
    }
}
