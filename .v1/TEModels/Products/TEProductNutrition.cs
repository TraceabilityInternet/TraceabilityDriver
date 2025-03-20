using TraceabilityEngine.Util;
using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductNutrition : ITEProductNutrition
    {
        [DisplayName("Nutrient Base Quantity Type")]
        public NutrientBaseQuantityType NutrientBaseQuantityType { get; set; }

        [DisplayName("Nutrient Base Quantity")]
        public TEMeasurement NutrientBaseQuantity { get; set; }

        [DisplayName("Nutrient Measurement Precision")]
        public GS1MeasurementPrecision NutritentMeasurementPrecision { get; set; }

        [DisplayName("Kilo-Calories")]
        public ITEProductNutritionCalorieMeasurement EnergyCalories { get; set; } = new TEProductNutritionCalorieMeasurement();

        [DisplayName("Kilo-Joules")]
        public ITEProductNutritionJoulesMeasurement EnergyJoules { get; set; } = new TEProductNutritionJoulesMeasurement();

        [DisplayName("Kilo-Calories from Fat")]
        public ITEProductNutritionCalorieMeasurement EnergyFromFatCalories { get; set; } = new TEProductNutritionCalorieMeasurement();

        [DisplayName("Kilo-Joules from Fat")]
        public ITEProductNutritionJoulesMeasurement EnergyFromFatJoules { get; set; } = new TEProductNutritionJoulesMeasurement();

        [DisplayName("Fat")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement Fat { get; set; }

        [DisplayName("Saturated Fat")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement SaturatedFat { get; set; }

        [DisplayName("Trans Fat")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement TransFat { get; set; }

        [DisplayName("Monosaturated Fat")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement MonoSaturatedFat { get; set; }

        [DisplayName("Polyunsaturated Fat")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement PolyUnsaturatedFat { get; set; }

        [DisplayName("Cholesterol")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement Cholesterol { get; set; }

        [DisplayName("Carbohydrates")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement Carbohydtrates { get; set; }

        [DisplayName("Sugars")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement Sugars { get; set; }

        [DisplayName("Polyols")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement Polyols { get; set; }

        [DisplayName("Starch")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement Starch { get; set; }

        [DisplayName("Salt")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement Salt { get; set; }

        [DisplayName("Sodium")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement Sodium { get; set; }

        [DisplayName("Fibre")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement Fibre { get; set; }

        [DisplayName("Protein")]
        [NutrientDefaultUnit("GTM")]
        public ITEProductNutritionWeightMeasurement Protein { get; set; }

        [DisplayName("Vitamin A")]
        [NutrientDefaultUnit("MC")]
        public ITEProductNutritionWeightMeasurement VitaminA { get; set; }

        [DisplayName("Vitamin D")]
        [NutrientDefaultUnit("MC")]
        public ITEProductNutritionWeightMeasurement VitaminD { get; set; }

        [DisplayName("Vitamin E")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement VitaminE { get; set; }

        [DisplayName("Vitamin K")]
        [NutrientDefaultUnit("MC")]
        public ITEProductNutritionWeightMeasurement VitaminK { get; set; }

        [DisplayName("Vitamin C")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement VitaminC { get; set; }

        [DisplayName("Thiamin")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Thiamin { get; set; }

        [DisplayName("Riboflavin")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Riboflavin { get; set; }

        [DisplayName("Niacin")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Niacin { get; set; }

        [DisplayName("Vitamin B6")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement VitaminB6 { get; set; }

        [DisplayName("Folic Acid")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement FolicAcid { get; set; }

        [DisplayName("Vitamin B12")]
        [NutrientDefaultUnit("MC")]
        public ITEProductNutritionWeightMeasurement VitaminB12 { get; set; }

        [DisplayName("Biotin")]
        [NutrientDefaultUnit("MC")]
        public ITEProductNutritionWeightMeasurement Biotin { get; set; }

        [DisplayName("Pantothenic Acid")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement PantothenicAcid { get; set; }

        [DisplayName("Potassium")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Potassium { get; set; }

        [DisplayName("Chloride")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Chloride { get; set; }

        [DisplayName("Calcium")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Calcium { get; set; }

        [DisplayName("Phosphorus")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Phosphorus { get; set; }

        [DisplayName("Magnesium")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Magnesium { get; set; }

        [DisplayName("Iron")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Iron { get; set; }

        [DisplayName("Zinc")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Zinc { get; set; }

        [DisplayName("Copper")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Copper { get; set; }

        [DisplayName("Manganese")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Manganese { get; set; }

        [DisplayName("Fluoride")]
        [NutrientDefaultUnit("MGM")]
        public ITEProductNutritionWeightMeasurement Fluoride { get; set; }

        [DisplayName("Selenium")]
        [NutrientDefaultUnit("MC")]
        public ITEProductNutritionWeightMeasurement Selenium { get; set; }

        [DisplayName("Chromium")]
        [NutrientDefaultUnit("MC")]
        public ITEProductNutritionWeightMeasurement Chromium { get; set; }

        [DisplayName("Molybdenum")]
        [NutrientDefaultUnit("MC")]
        public ITEProductNutritionWeightMeasurement Molybdenum { get; set; }

        [DisplayName("Iodine")]
        [NutrientDefaultUnit("MC")]
        public ITEProductNutritionWeightMeasurement Iodine { get; set; }
    }


}
