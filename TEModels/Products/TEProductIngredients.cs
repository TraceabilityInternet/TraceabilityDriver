using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductIngredients : ITEProductIngredients
    {
        public string Ingredients { get; set; }
        public List<string> IngredientsOfConcern { get; set; }
        public SourceAnimal SourceAnimal { get; set; }
        public PreservationTechnique PreservationTechnique { get; set; }
        public double JuiceContentPercent { get; set; }
        public NonBinaryLogicCode IsInstant { get; set; }
        public NonBinaryLogicCode IsSliced { get; set; }
        public FoodBeverageTargetUse MealType { get; set; }
        public string ServingSuggestion { get; set; }
        public string ServingSizeDescription { get; set; }
        public TEMeasurement ServingSize { get; set; }
        public GS1MeasurementPrecision NumberOfServingsPerPackagePrecision { get; set; }
        public double NumberOfServingsPerPackage { get; set; }
        public string NumberOfServingsRangeDescription { get; set; }
        public List<ITEDietType> DietTypes { get; set; } = new List<ITEDietType>();
    }

    
}
