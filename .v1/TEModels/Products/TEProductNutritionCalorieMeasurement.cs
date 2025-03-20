using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductNutritionCalorieMeasurement : ITEProductNutritionCalorieMeasurement
    {
        public bool IsEmpty()
        {
            if(this.Calories == 0 && this.DailyPercent == 0)
            {
                return true;
            }
            return false;
        }

        public double Calories { get; set; }
        public double DailyPercent { get; set; }
    }
}
