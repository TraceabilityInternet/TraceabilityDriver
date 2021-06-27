using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEDietType
    {
        DietTypeCode DietTypeCode { get; set; }
        string DietTypeSubCode { get; set; }
    }
}
