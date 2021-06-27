using TraceabilityEngine.Util.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductNutritionWeightMeasurement
    {
        public static bool IsNullOrEmpty(ITEProductNutritionWeightMeasurement weightMeasure)
        {
            if (weightMeasure == null || weightMeasure.IsEmpty())
            {
                return true;
            }
            return false;
        }

        bool IsEmpty();
        TEMeasurement Weight { get; set; }
        double DailyPercent { get; set; }
    }
}
