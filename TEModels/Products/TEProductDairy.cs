using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductDairy : ITEProductDairy
    {
        public CheeseFirmnessCode CheeseFirmness { get; set; }
        public SharpnessOfCheeseCode SharpnessOfCheese { get; set; }
        public string CheeseMaturationPeriodDescription { get; set; }
        public TEMeasurement MaturationTime { get; set; }
        public double FatPercentageInDryMaterial { get; set; }
        public double FatContentInMilk { get; set; }
        public NonBinaryLogicCode IsA2 { get; set; }
        public NonBinaryLogicCode IsHomogenised { get; set; }
        public NonBinaryLogicCode IsEdibleRind { get; set; }
    }
}
