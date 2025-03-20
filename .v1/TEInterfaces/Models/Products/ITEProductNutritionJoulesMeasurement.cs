using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductNutritionJoulesMeasurement
    {
        public static bool IsNullOrEmpty(ITEProductNutritionJoulesMeasurement joulesMeasure)
        {
            if (joulesMeasure == null || joulesMeasure.IsEmpty())
            {
                return true;
            }
            return false;
        }

        bool IsEmpty();
        double Joules { get; set; }
        double DailyPercent { get; set; }
    }
}
