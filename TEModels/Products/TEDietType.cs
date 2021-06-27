using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEDietType : ITEDietType
    {
        public DietTypeCode DietTypeCode { get; set; }
        public string DietTypeSubCode { get; set; }
    }
}
