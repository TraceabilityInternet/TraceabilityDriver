using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductAllergen : ITEProductAllergen
    {
        public LevelOfContainmentCode LevelOfContainment { get; set; }
        public AllergenCode Allergen { get; set; }
    }
}
