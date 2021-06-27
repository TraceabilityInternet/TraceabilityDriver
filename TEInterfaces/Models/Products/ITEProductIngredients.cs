using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductIngredients
    {
        string Ingredients { get; set; }
        List<string> IngredientsOfConcern { get; set; }
        SourceAnimal SourceAnimal { get; set; }
        PreservationTechnique PreservationTechnique { get; set; }
        double JuiceContentPercent { get; set; }
        NonBinaryLogicCode IsInstant { get; set; }
        NonBinaryLogicCode IsSliced { get; set; }
        FoodBeverageTargetUse MealType { get; set; }
        string ServingSuggestion { get; set; }
        string ServingSizeDescription { get; set; }
        TEMeasurement ServingSize { get; set; }
        GS1MeasurementPrecision NumberOfServingsPerPackagePrecision { get; set; }
        double NumberOfServingsPerPackage { get; set; }
        string NumberOfServingsRangeDescription { get; set; }
        List<ITEDietType> DietTypes { get; set; }
    }
}
