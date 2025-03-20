using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductFruitsAndVegetables
    {
        NonBinaryLogicCode IsPittedOrStoned { get; set; }
        NonBinaryLogicCode IsSeedless { get; set; }
        NonBinaryLogicCode IsShelledOrPeeled { get; set; }
        NonBinaryLogicCode IsWashedAndReadyToEat { get; set; }
        MaturationMethodCode MaturationMethod { get; set; }
    }
}
