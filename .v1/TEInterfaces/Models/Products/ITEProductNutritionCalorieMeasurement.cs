using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductNutritionCalorieMeasurement
    {
        public static bool IsNullOrEmpty(ITEProductNutritionCalorieMeasurement calmeasure)
        {
            if (calmeasure == null || calmeasure.IsEmpty())
            {
                return true;
            }
            return false;
        }

        bool IsEmpty();
        double Calories { get; set; }
        double DailyPercent { get; set; }
    }
}
