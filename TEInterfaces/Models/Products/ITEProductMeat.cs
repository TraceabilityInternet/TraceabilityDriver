using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductMeat
    {
        AnatomicalFormCode AnatomicalForm { get; set; }
        NonBinaryLogicCode BonelessClaim { get; set; }
        string TypeOfMeatAndPoultry { get; set; }
        TEMeasurement MinimumMeatContent { get; set; }
    }
}
