using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductDairy
    {
        CheeseFirmnessCode CheeseFirmness { get; set; }
        SharpnessOfCheeseCode SharpnessOfCheese { get; set; }
        string CheeseMaturationPeriodDescription { get; set; }
        TEMeasurement MaturationTime { get; set; }
        double FatPercentageInDryMaterial { get; set; }
        double FatContentInMilk { get; set; }
        NonBinaryLogicCode IsA2 { get; set; }
        NonBinaryLogicCode IsHomogenised { get; set; }
        NonBinaryLogicCode IsEdibleRind { get; set; }
    }
}
