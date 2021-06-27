using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.DB.DocumentDB;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProduct : ITEProductLite, ITEDocumentObject
    {
        ITEProductAllergensAndAdditives AllergensAndAdditives { get; set; }
        List<ITEAttachment> Attachments { get; set; }
        List<ITEProductAvailability> Availabilities { get; set; }
        ITEProductBeverage Beverage { get; set; }
        List<ITEProductCase> Cases { get; set; }
        List<GS1Category> Categories { get; set; }
        string Category { get; set; }
        List<ITECertificate> Certificates { get; set; }
        List<ITEProductChild> Children { get; set; }
        List<ProductClaim> Claims { get; set; }
        ITEProductDairy Dairy { get; set; }
        ITEProductFruitsAndVegetables FruitsAndVegetables { get; set; }
        List<ITEPhoto> Images { get; set; }
        ITEProductInfo Info { get; set; }
        ITEProductIngredients Ingredients { get; set; }
        ITEProductInstructions Instructions { get; set; }
        string InternalID { get; set; }
        ITEProductMeasurements Measurements { get; set; }
        ITEProductMeat Meat { get; set; }
        ITEProductNutrition Nutrition { get; set; }
        ITEProductPackaging Packaging { get; set; }
        ITEProductPreparation Preparation { get; set; }
        ProductTypeCode ProductType { get; set; }
        ITEProductProvenance Provenance { get; set; }
        ITEProductSeafood Seafood { get; set; }
        List<Species> Species { get; set; }
        List<ITEProductTreatment> Treatments { get; set; }
    }
}
