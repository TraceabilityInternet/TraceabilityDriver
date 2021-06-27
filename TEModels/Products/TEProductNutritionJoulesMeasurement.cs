using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductNutritionJoulesMeasurement : ITEProductNutritionJoulesMeasurement
    {
        public bool IsEmpty()
        {
            if (this.Joules == 0 && this.DailyPercent == 0)
            {
                return true;
            }
            return false;
        }
        public double Joules { get; set; }
        public double DailyPercent { get; set; }
    }
}
