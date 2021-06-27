using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductMeat : ITEProductMeat
    {
        public AnatomicalFormCode AnatomicalForm { get; set; }
        public NonBinaryLogicCode BonelessClaim { get; set; }
        public string TypeOfMeatAndPoultry { get; set; }
        public TEMeasurement MinimumMeatContent { get; set; }
    }
}
