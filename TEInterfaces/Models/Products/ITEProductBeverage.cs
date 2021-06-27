using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductBeverage
    {
        NonBinaryLogicCode IsCarbonated { get; set; }
        NonBinaryLogicCode IsDecaffeinated { get; set; }
        NonBinaryLogicCode IsFromConcentrate { get; set; }
        NonBinaryLogicCode IsVintage { get; set; }
        double PercentageOfAlcohol { get; set; }
        string Vintner { get; set; }
        string AlcoholicBeverageSubRegion { get; set; }
        string BeverageVintage { get; set; }
    }
}
