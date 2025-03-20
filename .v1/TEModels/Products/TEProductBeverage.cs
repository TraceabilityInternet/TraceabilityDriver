using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductBeverage : ITEProductBeverage
    {
        public NonBinaryLogicCode IsCarbonated { get; set; }
        public NonBinaryLogicCode IsDecaffeinated { get; set; }
        public NonBinaryLogicCode IsFromConcentrate { get; set; }
        public NonBinaryLogicCode IsVintage { get; set; }
        public double PercentageOfAlcohol { get; set; }
        public string Vintner { get; set; }
        public string AlcoholicBeverageSubRegion { get; set; }
        public string BeverageVintage { get; set; }
    }
}
