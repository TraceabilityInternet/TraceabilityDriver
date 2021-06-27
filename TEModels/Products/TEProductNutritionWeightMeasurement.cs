using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductNutritionWeightMeasurement : ITEProductNutritionWeightMeasurement
    {
        public bool IsEmpty()
        {
            if (TEMeasurement.IsNullOrEmpty(this.Weight)
             || this.DailyPercent <= 0
             || this.Weight.Value <= 0)
            {
                return true;
            }
            return false;
        }
        public TEMeasurement Weight { get; set; } = new TEMeasurement();
        public double DailyPercent { get; set; }
    }
}
