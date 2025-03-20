using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductCase : ITEProductCase
    {
        public IGTIN CaseGTIN { get; set; }
        public string CaseName { get; set; }
        public TEMeasurement Height { get; set; } = new TEMeasurement();
        public TEMeasurement Width { get; set; } = new TEMeasurement();
        public TEMeasurement Length { get; set; } = new TEMeasurement();
        public int InnerProductCount { get; set; }
        public TEMeasurement NetWeight { get; set; } = new TEMeasurement();
    }
}
