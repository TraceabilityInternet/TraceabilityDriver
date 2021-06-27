using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductFruitsAndVegetables : ITEProductFruitsAndVegetables
    {
        public NonBinaryLogicCode IsPittedOrStoned { get; set; }
        public NonBinaryLogicCode IsSeedless { get; set; }
        public NonBinaryLogicCode IsShelledOrPeeled { get; set; }
        public NonBinaryLogicCode IsWashedAndReadyToEat { get; set; }
        public MaturationMethodCode MaturationMethod { get; set; }
    }
}
