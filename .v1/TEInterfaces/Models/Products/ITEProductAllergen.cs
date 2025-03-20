using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductAllergen
    {
        public LevelOfContainmentCode LevelOfContainment { get; set; }
        public AllergenCode Allergen { get; set; }
    }
}
