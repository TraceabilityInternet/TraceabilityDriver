using TraceabilityEngine.Util;
using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Models;
using TraceabilityEngine.Models.Products;
using TraceabilityEngine.StaticData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Models.Identifiers;

namespace TraceabilityEngine.Mappers
{
    public class ProductWebVocabMapper : GS1WebVocabMapper, ITEProductMapper
    {
        public ITEProduct ConvertToProduct(string jsonStr)
        {
            // deserialize the json into a product here...
            try
            {
                JObject json = JObject.Parse(jsonStr);

                // base information
                ITEProduct product = new TEProduct();
                product.ProductType = ProductTypeCode.GetFromKey(GetStringValue(json, "@type")); //Null?
                product.InternalID = GetStringValue(json, "internalID");
                product.Category = GetStringValue(json, "category");
                product.Description = GetStringValue(json, "productDescription");
                product.Name = GetStringValue(json, "productName");
                product.GTIN = new GTIN(GetStringValue(json, "gtin"));

                // info
                product.Info = GetInfo(json);

                // species
                product.Species = GetSpecies(json, "gs1:species");

                // measurements
                product.Measurements = GetMeasurements(json);

                // colour codes
                GetColourCode(json, product);

                // product classification
                GetAdditionalProductClassification(json, product);

                // consumer sales conditions
                GetConsumerSalesConditions(json, product);

                // size codes
                GetSizeCode(json, product);
                
                // packaging
                GetPackaging(json, product);

                // images 
                product.Images = GetPhotos(json, "gs1:image");

                // instructions
                product.Instructions = GetInstructions(json);

                // preparation
                product.Preparation = GetPreparation(json);

                // provenance and claims
                product.Provenance = GetProvenance(json);

                // claims
                product.Claims = new List<ProductClaim>();
                if (json["productClaim"] is JArray)
                {
                    JArray jClaims = json["productClaim"] as JArray;
                    foreach (JObject jClaim in jClaims)
                    {
                        string key = GetIDAsString(jClaim, "claim");
                        ProductClaim claim = ProductClaim.GetFromKey(key);
                        if (claim != null)
                        {
                            product.Claims.Add(claim);
                        }
                    }
                }

                // categories
                product.Categories = new List<GS1Category>();
                if (json["productCategory"] is JArray)
                {
                    JArray jCategories = json["productCategory"] as JArray;
                    foreach (JObject jCat in jCategories)
                    {
                        string key = GetIDAsString(jCat, "category");
                        GS1Category category = GS1Category.GetFromKey(key);
                        if (category != null)
                        {
                            product.Categories.Add(category);
                        }
                    }
                }

                // treatments
                product.Treatments = new List<ITEProductTreatment>();
                foreach (JObject item in json["gs1:treatments"])
                {
                    TEProductTreatment treatment = new TEProductTreatment();
                    treatment.TreatmentType = TreatmentType.GetFromKey(GetStringValue(item, "gs1:foodTreatment"));
                    treatment.Time = GetMeasurement(item, "gs1:treatmentTime");
                    treatment.Concentration = GetMeasurement(item, "gs1:treatmentConcentration");
                    treatment.Temperature = GetMeasurement(item, "gs1:treatmentTemperature");
                    product.Treatments.Add(treatment);
                }

                // availabilities
                product.Availabilities = new List<ITEProductAvailability>();
                foreach (JObject item in json["gs1:productAvailabilities"])
                {
                    ITEProductAvailability availability = new TEProductAvailability()
                    {
                        GLN = IdentifierFactory.ParseGLN(GetStringValue((JObject)item["gs1:productAvailability"], "gs1:globalLocationNumber"), out string error),
                        Price = GetDoubleValue((JObject)item["gs1:productAvailability"], "gs1:price")
                    };
                    product.Availabilities.Add(availability);
                }

                // cases
                product.Cases = GetCases(json, "gs1:cases");

                // children
                product.Children = GetChildren(json);

                // certifications
                product.Certificates = GetCertificates(json, "gs1:certification");

                // attachments
                product.Attachments = GetAttachments(json, "referencedFile");

                // seafood 
                product.Seafood = GetSeafood(json, "seafood");

                // beverage
                product.Beverage = GetBeverage(json);

                // dairy
                product.Dairy = GetDairy(json);

                // meat
                product.Meat = GetMeat(json);

                // fruits and vegetables
                product.FruitsAndVegetables = GetFruitsAndVegetables(json);

                // ingredients
                product.Ingredients = GetIngredients(json);

                // nutrition
                product.Nutrition = GetNutrition(json);

                // additives and allergens
                product.AllergensAndAdditives = GetAdditivesAndAllergens(json);

                return product;
            }
            catch (Exception Ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                TELogger.Log(0, Ex);
                throw;
            }
        }
        public string ConvertFromProduct(ITEProduct product)
        {
            try
            {
                if (product == null) throw new ArgumentNullException(nameof(product));
                JObject json = new JObject();

                // info
                SetContext(json);
                SetString(json, "@type", product.ProductType?.Key); //Null? Check product definition on site
                
                SetLanguageString(json, "productName", product.Name);
                SetString(json, "internalID", product.InternalID);
                SetString(json, "category", product.Category);
                SetString(json, "gtin", product.GTIN?.ToString());
                SetLanguageString(json, "productDescription", product.Description);
                SetBoolAsObject(json, "isVariableWeight", product.Measurements.IsVariableWeight);

                // brand
                SetBrand(json, product);

                // info
                SetInfo(json, product.Info);

                // species
                SetSpecies(json, "gs1:species", product.Species);

                // seafood
                SetSeafood(json, "seafood", product.Seafood);

                // beverage
                SetBeverage(json, product.Beverage);

                // dairy
                SetDairy(json, product.Dairy);

                // fruits and vegetables
                SetFruitsAndVegetables(json, product.FruitsAndVegetables);

                // meta
                SetMeat(json, product.Meat);

                // additives and allergens
                SetAdditivesAndAllergens(json, product.AllergensAndAdditives);

                // images
                SetPhotos(json, "gs1:image", product.Images);

                // claims
                if (product.Claims != null && product.Claims.Count > 0)
                {
                    JArray jClaims = new JArray();
                    foreach (ProductClaim claim in product.Claims)
                    {
                        JObject jClaim = new JObject();
                        jClaim["@type"] = "gs1:productClaim";
                        SetIDAsString(jClaim, "claim", claim.Key);
                        jClaims.Add(jClaim);
                    }
                    json["productClaim"] = jClaims;
                }

                // categories
                if (product.Categories != null && product.Categories.Count > 0)
                {
                    JArray jCategories = new JArray();
                    foreach (GS1Category claim in product.Categories)
                    {
                        JObject jCat = new JObject();
                        jCat["@type"] = "gs1:productCategory";
                        SetIDAsString(jCat, "category", claim.Key);
                        jCategories.Add(jCat);
                    }
                    json["productCategory"] = jCategories;
                }

                // measurements
                SetMeasurements(json, product.Measurements);

                // colour codes
                SetColourCode(json, product);
                
                // product classifications
                SetAdditionalProductClassification(json, product);

                // sales conditions
                SetConsumerSalesConditions(json, product);

                // target markets
                //SetTargetMarket(json, product); TARGET MARKET PROPERTY NOT IMPLEMENTED YET

                // size codes
                SetSizeCode(json, product);

                // packaging
                SetPackaging(json, product);

                // intructions
                SetInstructions(json, product.Instructions);

                // preparation
                SetPreparation(json, product.Preparation);

                // provenance
                SetProvenance(json, product.Provenance);

                // treatments
                SetProductTreatments(json, product.Treatments);

                // availabilities
                SetProductAvailabilities(json, product.Availabilities);

                // certificates
                SetCertificates(json, product.Certificates, "gs1:certification");

                // attachments
                SetAttachments(json, "referencedFile", product.Attachments);

                // cases
                SetCases(json, "gs1:cases", product.Cases);

                // children
                SetChildren(json, product.Children);

                // ingredients
                SetIngredients(json, product.Ingredients);

                // nutrition
                SetNutrition(json, product.Nutrition);

                // diet codes
                SetDietCode(json, product);

                return json.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception Ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private List<ITEProductChild> GetChildren(JObject json)
        {
            List<ITEProductChild> children = new List<ITEProductChild>();
            if (json?["children"] is JArray)
            {
                JArray jChildren = json["children"] as JArray;
                foreach (JObject jChild in jChildren)
                {
                    ITEProductChild child = new TEProductChild();
                    child.ChildGTIN = IdentifierFactory.ParseGTIN(GetStringValue(jChild, "gtin"), out string error);
                    child.InnerProductCount = GetIntValue(jChild, "count");
                    children.Add(child);
                }
            }
            return children;
        }
        private void SetChildren(JObject json, List<ITEProductChild> children)
        {
            if (json != null && children != null)
            {
                JArray jChildren = new JArray();

                foreach (ITEProductChild child in children)
                {
                    JObject jChild = new JObject();
                    SetString(jChild, "gtin", child.ChildGTIN?.ToString());
                    SetInteger(jChild, "count", child.InnerProductCount);
                    jChildren.Add(jChild);
                }

                json["children"] = jChildren;
            }
        }

        private ITEProductNutrition GetNutrition(JObject json)
        {
            ITEProductNutrition nutrition = new TEProductNutrition();

            if (json != null)
            {
                //nutrition.NutrientBaseQuantityType = NutrientBaseQuantityType. How to handle enums?
                //nutrition.NutritentMeasurementPrecision = GS1MeasurementPrecision.
                nutrition.NutrientBaseQuantity = new TEMeasurement() //Only JSON property for measurements recorded like this for some reason.
                {
                    Value = Convert.ToDouble(GetStringValue((JObject)json["nutrientBasisQuantity"], "value")),
                    UoM = UOM.LookUpFromUNCode(GetStringValue((JObject)json["nutrientBasisQuantity"], "unitCode"))
                };

                if (json?["energyPerNutrientBasis"] is JArray)
                {
                    foreach (JObject item in json["energyPerNutrientBasis"])
                    {
                        if (item["unitCode"].ToString() == "D70")
                        {
                            nutrition.EnergyCalories = new TEProductNutritionCalorieMeasurement()
                            {
                                Calories = GetDoubleValue(item, "value"),
                                DailyPercent = GetDoubleValue(item, "dailyValueIntakePercent")
                            };
                        }
                        else if (item["unitCode"].ToString() == "JOU")
                        {
                            nutrition.EnergyJoules = new TEProductNutritionJoulesMeasurement()
                            {
                                Joules = GetDoubleValue(item, "value"),
                                DailyPercent = GetDoubleValue(item, "dailyValueIntakePercent")
                            };
                        }
                    }
                }

                if (json["energyFromFatPerNutrientBasis"] is JArray)
                {
                    foreach (JObject item in json["energyFromFatPerNutrientBasis"])
                    {
                        if (item["unitCode"].ToString() == "D70")
                        {
                            nutrition.EnergyFromFatCalories = new TEProductNutritionCalorieMeasurement()
                            {
                                Calories = GetDoubleValue(item, "value"),
                                DailyPercent = GetDoubleValue(item, "dailyValueIntakePercent")
                            };
                        }
                        else if (item["unitCode"].ToString() == "JOU")
                        {
                            nutrition.EnergyFromFatJoules = new TEProductNutritionJoulesMeasurement()
                            {
                                Joules = GetDoubleValue(item, "value"),
                                DailyPercent = GetDoubleValue(item, "dailyValueIntakePercent")
                            };
                        }
                    }
                }



                // Nutrition Label
                nutrition.Fat = GetNutrient(json, "fatPerNutrientBasis");
                nutrition.SaturatedFat = GetNutrient(json, "saturatedFatPerNutrientBasis");
                nutrition.TransFat = GetNutrient(json, "transFatPerNutrientBasis");
                nutrition.MonoSaturatedFat = GetNutrient(json, "monosaturatedFatPerNutrientBasis");
                nutrition.PolyUnsaturatedFat = GetNutrient(json, "polyunsaturatedFatPerNutrientBasis");
                nutrition.Cholesterol = GetNutrient(json, "cholesterolPerNutrientBasis");
                nutrition.Carbohydtrates = GetNutrient(json, "carbohydratesPerNutrientBasis");
                nutrition.Sugars = GetNutrient(json, "sugarsPerNutrientBasis");
                nutrition.Polyols = GetNutrient(json, "polyolsPerNutrientBasis");
                nutrition.Starch = GetNutrient(json, "starchPerNutrientBasis");
                nutrition.Salt = GetNutrient(json, "saltPerNutrientBasis");
                nutrition.Sodium = GetNutrient(json, "sodiumPerNutrientBasis");
                nutrition.Fibre = GetNutrient(json, "fibrePerNutrientBasis");
                nutrition.Protein = GetNutrient(json, "proteinPerNutrientBasis");
                nutrition.VitaminA = GetNutrient(json, "vitaminAPerNutrientBasis");
                nutrition.VitaminD = GetNutrient(json, "vitaminDPerNutrientBasis");
                nutrition.VitaminE = GetNutrient(json, "vitaminEPerNutrientBasis");
                nutrition.VitaminK = GetNutrient(json, "vitaminKPerNutrientBasis");
                nutrition.VitaminC = GetNutrient(json, "vitaminCPerNutrientBasis");
                nutrition.Thiamin = GetNutrient(json, "thiaminPerNutrientBasis");
                nutrition.Riboflavin = GetNutrient(json, "riboflavinPerNutrientBasis");
                nutrition.Niacin = GetNutrient(json, "niacinPerNutrientBasis");
                nutrition.VitaminB6 = GetNutrient(json, "vitaminB6PerNutrientBasis");
                nutrition.VitaminB12 = GetNutrient(json, "vitaminB12NutrientBasis");
                nutrition.FolicAcid = GetNutrient(json, "folicAcidPerNutrientBasis");
                nutrition.Biotin = GetNutrient(json, "biotinPerNutrientBasis");
                nutrition.PantothenicAcid = GetNutrient(json, "pantothenicAcidPerNutrientBasis");
                nutrition.Potassium = GetNutrient(json, "potassiumPerNutrientBasis");
                nutrition.Chloride = GetNutrient(json, "chloridePerNutrientBasis");
                nutrition.Calcium = GetNutrient(json, "calciumPerNutrientBasis");
                nutrition.Phosphorus = GetNutrient(json, "phosphorusPerNutrientBasis");
                nutrition.Magnesium = GetNutrient(json, "magnesiumPerNutrientBasis");
                nutrition.Iron = GetNutrient(json, "ironPerNutrientBasis");
                nutrition.Zinc = GetNutrient(json, "zincPerNutrientBasis");
                nutrition.Copper = GetNutrient(json, "copperPerNutrientBasis");
                nutrition.Manganese = GetNutrient(json, "manganesePerNutrientBasis");
                nutrition.Fluoride = GetNutrient(json, "fluoridePerNutrientBasis");
                nutrition.Selenium = GetNutrient(json, "seleniumPerNutrientBasis");
                nutrition.Chromium = GetNutrient(json, "chromiumPerNutrientBasis");
                nutrition.Molybdenum = GetNutrient(json, "molybdenumPerNutrientBasis");
                nutrition.Iodine = GetNutrient(json, "iodinePerNutrientBasis");
            }

            return nutrition;
        }
        private void SetNutrition(JObject json, ITEProductNutrition nutrition)
        {
            if (json != null && nutrition != null)
            {
                SetIDAsString(json, "nutrientBasisQuantityType", nutrition.NutrientBaseQuantityType.ToString()); //Missing "gs1:NutrientBasisQuantityCode-BY_MEASURE"
                SetIDAsString(json, "nutrientMeasurementPrecision", nutrition.NutritentMeasurementPrecision.ToString()); //Missing "gs1:MeasurementPrecisionCode-APPROXIMATELY"
                SetNutrientBasisQuantity(json, nutrition.NutrientBaseQuantity);
                SetPerNutrientBasis(json, "fatPerNutrientBasis", nutrition.Fat);
                SetPerNutrientBasis(json, "saturatedFatPerNutrientBasis", nutrition.SaturatedFat);
                SetPerNutrientBasis(json, "transFatPerNutrientBasis", nutrition.TransFat);
                // SetPerNutrientBasis(json, "monounsaturatedFatPerNutrientBasis", nutrition.MonoUnsaturatedFat);
                SetPerNutrientBasis(json, "monosaturatedFatPerNutrientBasis", nutrition.MonoSaturatedFat);
                SetPerNutrientBasis(json, "polyunsaturatedFatPerNutrientBasis", nutrition.PolyUnsaturatedFat);
                SetPerNutrientBasis(json, "cholesterolPerNutrientBasis", nutrition.Cholesterol);
                SetPerNutrientBasis(json, "carbohydratesPerNutrientBasis", nutrition.Carbohydtrates);
                SetPerNutrientBasis(json, "sugarsPerNutrientBasis", nutrition.Sugars);
                SetPerNutrientBasis(json, "polyolsPerNutrientBasis", nutrition.Polyols);
                SetPerNutrientBasis(json, "starchPerNutrientBasis", nutrition.Starch);
                SetPerNutrientBasis(json, "saltPerNutrientBasis", nutrition.Salt);
                SetPerNutrientBasis(json, "sodiumPerNutrientBasis", nutrition.Sodium);
                SetPerNutrientBasis(json, "fibrePerNutrientBasis", nutrition.Fibre);
                SetPerNutrientBasis(json, "proteinPerNutrientBasis", nutrition.Protein);
                SetPerNutrientBasis(json, "vitaminAPerNutrientBasis", nutrition.VitaminA);
                SetPerNutrientBasis(json, "vitaminDPerNutrientBasis", nutrition.VitaminD);
                SetPerNutrientBasis(json, "vitaminEPerNutrientBasis", nutrition.VitaminE);
                SetPerNutrientBasis(json, "vitaminKPerNutrientBasis", nutrition.VitaminK);
                SetPerNutrientBasis(json, "vitaminCPerNutrientBasis", nutrition.VitaminC);
                SetPerNutrientBasis(json, "thiaminPerNutrientBasis", nutrition.Thiamin);
                SetPerNutrientBasis(json, "riboflavinPerNutrientBasis", nutrition.Riboflavin);
                SetPerNutrientBasis(json, "niacinPerNutrientBasis", nutrition.Niacin);
                SetPerNutrientBasis(json, "vitaminB6PerNutrientBasis", nutrition.VitaminB6);
                SetPerNutrientBasis(json, "vitaminB12NutrientBasis", nutrition.VitaminB12);
                SetPerNutrientBasis(json, "folicAcidPerNutrientBasis", nutrition.FolicAcid);
                SetPerNutrientBasis(json, "biotinPerNutrientBasis", nutrition.Biotin);
                SetPerNutrientBasis(json, "pantothenicAcidPerNutrientBasis", nutrition.PantothenicAcid);
                SetPerNutrientBasis(json, "potassiumPerNutrientBasis", nutrition.Potassium);
                SetPerNutrientBasis(json, "chloridePerNutrientBasis", nutrition.Chloride);
                SetPerNutrientBasis(json, "calciumPerNutrientBasis", nutrition.Calcium);
                SetPerNutrientBasis(json, "phosphorusPerNutrientBasis", nutrition.Phosphorus);
                SetPerNutrientBasis(json, "magnesiumPerNutrientBasis", nutrition.Magnesium);
                SetPerNutrientBasis(json, "ironPerNutrientBasis", nutrition.Iron);
                SetPerNutrientBasis(json, "zincPerNutrientBasis", nutrition.Zinc);
                SetPerNutrientBasis(json, "copperPerNutrientBasis", nutrition.Copper);
                SetPerNutrientBasis(json, "manganesePerNutrientBasis", nutrition.Manganese);
                SetPerNutrientBasis(json, "fluoridePerNutrientBasis", nutrition.Fluoride);
                SetPerNutrientBasis(json, "seleniumPerNutrientBasis", nutrition.Selenium);
                SetPerNutrientBasis(json, "chromiumPerNutrientBasis", nutrition.Chromium);
                SetPerNutrientBasis(json, "molybdenumPerNutrientBasis", nutrition.Molybdenum);
                SetPerNutrientBasis(json, "iodinePerNutrientBasis", nutrition.Iodine);
                SetEnergyPerNutrientBasis(json, "energyPerNutrientBasis", nutrition);
                SetEnergyFatPerNutrientBasis(json, "energyFromFatPerNutrientBasis", nutrition);
            }
        }

        private ITEProductIngredients GetIngredients(JObject json)
        {
            ITEProductIngredients ingredients = new TEProductIngredients();
            
            if (json != null)
            {
                // ingredients of concern
                ingredients.IngredientsOfConcern = new List<string>();
                if (json["ingredientsOfConcern"] is JArray)
                {
                    JArray jArr = json["ingredientsOfConcern"] as JArray;
                    foreach (JToken jt in jArr)
                    {
                        if (jt is JArray)
                        {
                            JArray jLangArr = jt as JArray;
                            if (jLangArr.Count > 0)
                            {
                                if (jLangArr[0] is JObject)
                                {
                                    JObject jLang = jLangArr[0] as JObject;
                                    string ingredient = jLang.Value<string>("@value");
                                    if (!string.IsNullOrWhiteSpace(ingredient))
                                    {
                                        ingredients.IngredientsOfConcern.Add(ingredient);
                                    }
                                }
                            }
                        }
                    }
                }

                ingredients.Ingredients = GetStringValue(json, "ingredientStatement");
                ingredients.SourceAnimal = SourceAnimal.GetFromKey(GetIDAsString(json, "sourceAnimal"));
                ingredients.PreservationTechnique = PreservationTechnique.GetFromKey(GetIDAsString(json, "preservationTechnique"));
                ingredients.JuiceContentPercent = GetDoubleValue(json, "juiceContentPercent");
                ingredients.IsInstant = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isInstant"));
                ingredients.IsSliced = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isSliced"));
                ingredients.MealType = FoodBeverageTargetUse.GetFromKey(GetIDAsString(json, "foodBeverageTargetUse"));
                ingredients.ServingSuggestion = GetStringValue(json, "servingSuggestion");
                ingredients.ServingSizeDescription = GetStringValue(json, "servingSizeDescription");
                ingredients.ServingSize = GetMeasurement(json, "servingSize");
                ingredients.NumberOfServingsPerPackage = GetDoubleValue(json, "numberOfServingsPerPackage");
                //ingredients.NumberOfServingsPerPackagePrecision = GS1MeasurementPrecision needs get from key method.
                ingredients.NumberOfServingsRangeDescription = GetStringValue(json, "numberOfServingsRangeDescription");
                //ingredients.IngredientsOfConcern = GetStringValue((JObject)json["ingredientOfConcern"][0][0], "@value");

                ingredients.DietTypes = new List<ITEDietType>();
                if (json["dietCode"] is JArray)
                {
                    foreach (JObject item in json["dietCode"])
                    {
                        ITEDietType diet = new TEDietType()
                        {
                            DietTypeCode = DietTypeCode.GetFromKey(GetIDAsString(item, "dietType")),
                            DietTypeSubCode = GetStringValue(item, "dietTypeSubcode")
                        };
                        ingredients.DietTypes.Add(diet);
                    }
                }
            }

            return ingredients;
        }
        private void SetIngredients(JObject json, ITEProductIngredients ingredients)
        {
            if (json != null && ingredients != null)
            {
                // ingredients of concern
                if (ingredients.IngredientsOfConcern != null)
                {
                    JArray jArr = new JArray();
                    foreach (string ingredient in ingredients.IngredientsOfConcern)
                    {
                        JArray jLang = new JArray();
                        JObject jLangObj = new JObject();
                        jLangObj["@value"] = ingredient;
                        jLangObj["@language"] = "en";
                        jLang.Add(jLangObj);
                        jArr.Add(jLang);
                    }
                    json["ingredientsOfConcern"] = jArr;
                }

                SetLanguageString(json, "ingredientStatement", ingredients.Ingredients);
                SetIDAsArray(json, "sourceAnimal", ingredients.SourceAnimal);
                SetIDAsString(json, "preservationTechnique", ingredients.PreservationTechnique);
                SetDouble(json, "juiceContentPercent", ingredients.JuiceContentPercent); //CHECK PROPERTY ISN'T NULL?
                SetIDAsString(json, "isInstant", ingredients.IsInstant);
                SetIDAsString(json, "isSliced", ingredients.IsSliced);
                SetIDAsString(json, "foodBeverageTargetUse", ingredients.MealType);
                SetLanguageString(json, "servingSuggestion", ingredients.ServingSuggestion);
                SetLanguageString(json, "servingSizeDescription", ingredients.ServingSizeDescription);
                SetMeasurement(json, "servingSize", ingredients.ServingSize); //IS UN code for GRAM a typo? GTN vs GRM
                SetDouble(json, "numberOfServingsPerPackage", ingredients.NumberOfServingsPerPackage);
                SetIDAsString(json, "numberOfServingsPerPackageMeasurementPrecision", ingredients.NumberOfServingsPerPackagePrecision.ToString()); //Missing "gs1:MeasurementPrecisionCode-APPROXIMATELY"
                SetLanguageString(json, "numberOfServingsRangeDescription", ingredients.NumberOfServingsRangeDescription);
            }
        }

        private ITEProductProvenance GetProvenance(JObject json)
        {
            ITEProductProvenance prov = new TEProductProvenance();

            if (json != null)
            {
                prov.ProductFeatureBenefit = GetStringValue(json, "productFeatureBenefit");
                prov.CountriesOfOrigin = GetCountryFromArray(json, "countryOfOrigin");
                prov.CountriesOfAssembly = GetCountryFromArray(json, "countryOfAssembly");
                prov.CountriesOfProcessing = GetCountryFromArray(json, "countryOfProcessing");
                prov.CountryOfLastProcessing = GetCountryFromArray(json, "countryOfLastProcessing").LastOrDefault();
                prov.CountryOfOriginStatement = GetLanguageString(json, "countryOfOriginStatement");

                //prov.CountriesOfProcessing Not Impltemented Yet
                prov.ProvenanceStatement = GetStringValue(json, "provenanceStatement");
                prov.HealthClaimDescription = GetStringValue(json, "healthClaimDescription");
                prov.NutritionalClaimDescription = GetStringValue(json, "nutritionalClaimStatement");
                prov.GMOContainmentStatus = LevelOfContainmentCode.GetFromKey(GetIDAsString(json, "gmoContainmentStatus"));
                prov.NutritionalClaims = new List<NutritionalClaimCode>();
                foreach (JObject item in json["nutritionalClaim"])
                {
                    NutritionalClaimCode code = new NutritionalClaimCode();
                    code = NutritionalClaimCode.GetFromKey(GetStringValue(item, "@id"));
                    prov.NutritionalClaims.Add(code);
                }
                prov.GrowingMethods = new List<GrowingMethod>();
                foreach (JObject item in json["growingMethod"])
                {
                    GrowingMethod method = new GrowingMethod();
                    method = GrowingMethod.GetFromKey(GetStringValue(item, "@id"));
                    prov.GrowingMethods.Add(method);
                }
                prov.IsIrradiated = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "irradiatedCode"));
                prov.MayBeReheated = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "reheatingClaim"));
                prov.RefrigerationInformation = FoodBeverageRefrigerationClaimCode.GetFromKey(GetIDAsString(json, "foodBeverageRefrigerationClaim"));
                prov.PackageAccreditationCodes = new List<PackageAccreditationCode>();
                foreach (JObject item in json["packagingMarkedLabelAccreditation"])
                {
                    PackageAccreditationCode code = new PackageAccreditationCode();
                    code = PackageAccreditationCode.GetFromKey(GetStringValue(item, "@id"));
                    prov.PackageAccreditationCodes.Add(code);
                }
                prov.PackageFreeFromCodes = new List<PackageFreeFromCode>();
                foreach (JObject item in json["packagingMarkedFreeFrom"])
                {
                    PackageFreeFromCode code = new PackageFreeFromCode();
                    code = PackageFreeFromCode.GetFromKey(GetStringValue(item, "@id"));
                    prov.PackageFreeFromCodes.Add(code);
                }
                prov.OrganicClaims = new List<ITEProductOrganicClaim>();
                foreach (JObject item in json["organicClaim"])
                {
                    ITEProductOrganicClaim claim = new TEProductOrganicClaim();
                    claim.OrganicPercentage = GetDoubleValue(item, "organicClaimPercent");
                    claim.OrganicLabel = OrganicLabel.GetFromKey(GetIDAsString(item, "organicClaimAgency"));
                    prov.OrganicClaims.Add(claim);
                }
            }

            return prov;
        }
        private void SetProvenance(JObject json, ITEProductProvenance prov)
        {
            if (json != null && prov != null)
            {
                SetLanguageString(json, "productFeatureBenefit", prov.ProductFeatureBenefit);
                SetLanguageString(json, "countryOfOriginStatement", prov.CountryOfOriginStatement);
                SetCountryAsArray(json, "countryOfOrigin", prov.CountriesOfOrigin);
                SetCountryAsArray(json, "countryOfProcessing", prov.CountriesOfProcessing);
                SetCountryAsArray(json, "countryOfAssembly", prov.CountriesOfAssembly);
                SetCountryAsArray(json, "countryOfLastProcessing", prov.CountryOfLastProcessing);
                SetLanguageString(json, "provenanceStatement", prov.ProvenanceStatement);
                SetLanguageString(json, "healthClaimDescription", prov.HealthClaimDescription);
                SetLanguageString(json, "nutritionalClaimStatement", prov.NutritionalClaimDescription);
                if (prov.NutritionalClaims != null)
                {
                    JArray jClaims = new JArray();
                    foreach (NutritionalClaimCode claim in prov.NutritionalClaims)
                    {
                        JObject jClaim = new JObject();
                        SetString(jClaim, "@id", claim.Key);
                        jClaims.Add(jClaim);
                    }
                    json["nutritionalClaim"] = jClaims;
                }
                if (prov.GrowingMethods != null)
                {
                    JArray jGrowingMethods = new JArray();
                    foreach (GrowingMethod growingMethod in prov.GrowingMethods)
                    {
                        JObject jGrowingMethod = new JObject();
                        SetString(jGrowingMethod, "@id", growingMethod.Key);
                        jGrowingMethods.Add(jGrowingMethod);
                    }
                    json["growingMethod"] = jGrowingMethods;
                }
                SetIDAsString(json, "irradiatedCode", prov.IsIrradiated); //Check output, "gs1:NonbinaryLogicCode-FALSE"
                SetIDAsString(json, "reheatingClaim", prov.MayBeReheated); //Check output, "gs1:NonbinaryLogicCode-FALSE"
                SetIDAsString(json, "foodBeverageRefrigerationClaim", prov.RefrigerationInformation);
                SetIDAsString(json, "gmoContainmentStatus", prov.GMOContainmentStatus);
                if (prov.GrowingMethods != null)
                {
                    JArray jCodes = new JArray();
                    foreach (PackageAccreditationCode code in prov.PackageAccreditationCodes)
                    {
                        JObject jCode = new JObject();
                        SetString(jCode, "@id", code.Key);
                        jCodes.Add(jCode);
                    }
                    json["packagingMarkedLabelAccreditation"] = jCodes;
                }
                if (prov.PackageFreeFromCodes != null)
                {
                    JArray jCodes = new JArray();
                    foreach (PackageFreeFromCode code in prov.PackageFreeFromCodes)
                    {
                        JObject jCode = new JObject();
                        SetString(jCode, "@id", code.Key);
                        jCodes.Add(jCode);
                    }
                    json["packagingMarkedFreeFrom"] = jCodes;
                }
                //MISSING "packagingMarkedDietAllergenType" property
                if (prov.OrganicClaims != null)
                {
                    JArray jClaims = new JArray();
                    foreach (TEProductOrganicClaim claim in prov.OrganicClaims)
                    {
                        JObject jClaim = new JObject();
                        SetDouble(jClaim, "organicClaimPercent", claim.OrganicPercentage);
                        SetIDAsString(jClaim, "organicClaimAgency", claim.OrganicLabel);
                        SetString(jClaim, "@type", "gs1:OrganicClaimDetails");
                        jClaims.Add(jClaim);
                    }
                    json["organicClaim"] = jClaims;
                }
            }
        }

        private ITEProductPreparation GetPreparation(JObject json)
        {
            ITEProductPreparation prepare = new TEProductPreparation();

            if (json["preparationInformation"] is JObject)
            {
                JObject jPrepare = json["preparationInformation"] as JObject;

                prepare.PreparationInstructions = GetLanguageString(jPrepare, "preparationInstructions");
                prepare.PreparationConsumptionPrecautions = GetLanguageString(jPrepare, "preparationConsumptionPrecautions");
                prepare.ManufacturerPreparationTypeCode = PreparationTypeCode.GetFromKey(GetIDAsString(jPrepare, "manufacturerPreparationCode"));
                prepare.PreparationTypeCode = PreparationTypeCode.GetFromKey(GetIDAsString(jPrepare, "preparationCode"));
                prepare.ConvenienceLevelPercent = GetDoubleValue(jPrepare, "convenienceLevelPercent");
                prepare.OptimumConsumptionMinTemperature = GetMeasurement(jPrepare, "minimumOptimumConsumptionTemperature");
                prepare.OptimumConsumptionMaxTemperature = GetMeasurement(jPrepare, "maximumOptimumConsumptionTemperature");
                prepare.ProductYields = GetProductYields(jPrepare);
            }

            return prepare;
        }
        private void SetPreparation(JObject json, ITEProductPreparation prepare)
        {
            if (json != null && prepare != null)
            {
                JObject jPrepare = (json["preparationInformation"] = new JObject()) as JObject;
                SetLanguageString(jPrepare, "preparationInstructions", prepare.PreparationInstructions);
                SetLanguageString(jPrepare, "preparationConsumptionPrecautions", prepare.PreparationConsumptionPrecautions);
                SetIDAsString(jPrepare, "manufacturerPreparationCode", prepare.ManufacturerPreparationTypeCode);
                SetIDAsString(jPrepare, "preparationCode", prepare.PreparationTypeCode);
                SetDouble(jPrepare, "convenienceLevelPercent", prepare.ConvenienceLevelPercent);
                SetMeasurement(jPrepare, "minimumOptimumConsumptionTemperature", prepare.OptimumConsumptionMinTemperature);
                SetMeasurement(jPrepare, "maximumOptimumConsumptionTemperature", prepare.OptimumConsumptionMaxTemperature);
                SetString(jPrepare, "@type", "gs1:FoodAndBeveragePreparationInformation");
                if (prepare.ProductYields != null)
                {
                    JArray jYields = new JArray();
                    foreach (TEProductYield yield in prepare.ProductYields)
                    {
                        JObject jYield = new JObject();
                        SetIDAsString(jYield, "productYieldType", yield.ProductYieldType);
                        SetDouble(jYield, "productYieldVariationPercentage", yield.VariationPercent);
                        SetMeasurement(jYield, "productYield", yield.Amount);
                        SetString(jYield, "@type", "gs1:ProductYieldDetails");
                        jYields.Add(jYield);
                    }
                    json["preparationInformation"]["yield"] = jYields;
                }
            }
        }

        private ITEProductInstructions GetInstructions(JObject json)
        {
            ITEProductInstructions instructions = new TEProductInstructions();

            if (json != null)
            {
                instructions.WarningInformation = GetStringValue(json, "warningCopyDescription");
                instructions.ConsumerSafetyInformation = GetStringValue(json, "consumerSafetyInformation");
                instructions.ConsumerStorageInformation = GetStringValue(json, "consumerStorageInstructions");
                instructions.SupplierSpecifiedMinimumConsumerStorageDays = GetIntValue(json, "supplierSpecifiedMinimumConsumerStorageDays");
                instructions.ConsumerUsageInstructions = GetStringValue(json, "consumerUsageInstructions");
                instructions.ConsumerPackageDisclaimer = GetStringValue(json, "consumerPackageDisclaimer");
                instructions.CustomerSupportCenter = GetIDAsString(json, "customerSupportCentre");
                instructions.ConsumerHandlingInfoURL = GetReferencedFileDetails(json, "consumerHandlingStorage");
                instructions.RecipeWebsiteURL = GetReferencedFileDetails(json, "recipeWebsite");
                instructions.AudioFileURL = GetReferencedFileDetails(json, "audioFile");
                instructions.UserManualURL = GetReferencedFileDetails(json, "userManualURL");
            }

            return instructions;
        }
        private void SetInstructions(JObject json, ITEProductInstructions instructions)
        {
            if (json != null && instructions != null)
            {
                SetLanguageString(json, "warningCopyDescription", instructions.WarningInformation);
                SetLanguageString(json, "consumerSafetyInformation", instructions.ConsumerSafetyInformation);
                SetLanguageString(json, "consumerStorageInstructions", instructions.ConsumerStorageInformation);
                SetIntItem(json, "supplierSpecifiedMinimumConsumerStorageDays", instructions.SupplierSpecifiedMinimumConsumerStorageDays);
                SetLanguageString(json, "consumerUsageInstructions", instructions.ConsumerUsageInstructions);
                SetLanguageString(json, "consumerPackageDisclaimer", instructions.ConsumerPackageDisclaimer);
                SetIDAsString(json, "customerSupportCentre", instructions.CustomerSupportCenter);
                SetReferencedFileDetails(json, "consumerHandlingStorage", instructions.ConsumerHandlingInfoURL);
                //SetReferencedFileDetails(json, "instructionsForUse", instructions.InstructionsForUse); Property not implemented yet.
                SetReferencedFileDetails(json, "recipeWebsite", instructions.RecipeWebsiteURL);
                SetReferencedFileDetails(json, "audioFile", instructions.AudioFileURL);
                SetReferencedFileDetails(json, "userManualURL", instructions.UserManualURL);
            }
        }

        private ITEProductMeasurements GetMeasurements(JObject json)
        {
            ITEProductMeasurements measurements = new TEProductMeasurements();

            if (json != null)
            {
                measurements.NetWeight = GetMeasurement(json, "netWeight");
                measurements.GrossWeight = GetMeasurement(json, "grossWeight");
                measurements.DrainedWeight = GetMeasurement(json, "drainedWeight");
                measurements.NetContent = GetMeasurement(json, "netContent");
                measurements.PackagedHeight = GetMeasurement(json, "inPackageHeight");
                measurements.PackagedWidth = GetMeasurement(json, "inPackageWidth");
                measurements.PackagedLength = GetMeasurement(json, "inPackageDepth");
                measurements.PackagedDiameter = GetMeasurement(json, "inPackageDiameter");
                measurements.OutOfPackagedHeight = GetMeasurement(json, "outOfPackageHeight");
                measurements.OutOfPackagedWidth = GetMeasurement(json, "outOfPackageWidth");
                measurements.OutOfPackagedLength = GetMeasurement(json, "outOfPackageDepth");
                measurements.OutOfPackagedDiameter = GetMeasurement(json, "outOfPackageDiameter");
                measurements.SizeDescription = GetStringValue(json, "descriptiveSize");
                measurements.IsVariableWeight = GetBoolValue(json, "isVariableWeight") ?? false;
            }

            return measurements;
        }
        private void SetMeasurements(JObject json, ITEProductMeasurements measurements)
        {
            if (json != null && measurements != null)
            {
                SetMeasurement(json, "netWeight", measurements.NetWeight);
                SetMeasurement(json, "grossWeight", measurements.GrossWeight);
                SetMeasurement(json, "drainedWeight", measurements.DrainedWeight);
                SetMeasurement(json, "netContent", measurements.NetContent);
                SetMeasurement(json, "inPackageHeight", measurements.PackagedHeight);
                SetMeasurement(json, "inPackageWidth", measurements.PackagedWidth);
                SetMeasurement(json, "inPackageDepth", measurements.PackagedLength); //Length = depth?
                SetMeasurement(json, "inPackageDiameter", measurements.PackagedDiameter);
                SetMeasurement(json, "outOfPackageHeight", measurements.OutOfPackagedHeight);
                SetMeasurement(json, "outOfPackageWidth", measurements.OutOfPackagedWidth);
                SetMeasurement(json, "outOfPackageDepth", measurements.OutOfPackagedLength);
                SetMeasurement(json, "outOfPackageDiameter", measurements.OutOfPackagedDiameter);
                SetLanguageString(json, "descriptiveSize", measurements.SizeDescription);
            }
        }

        private ITEProductDairy GetDairy(JObject json)
        {
            ITEProductDairy dairy = new TEProductDairy();
            if (json != null)
            {
                dairy.IsA2 = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isA2Dairy"));
                dairy.IsHomogenised = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isHomogenised"));
                dairy.IsEdibleRind = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isRindEdible"));
                dairy.CheeseFirmness = CheeseFirmnessCode.GetFromKey(GetIDAsString(json, "cheeseFirmness"));
                dairy.SharpnessOfCheese = SharpnessOfCheeseCode.GetFromKey(GetIDAsString(json, "sharpnessOfCheese"));
                dairy.CheeseMaturationPeriodDescription = GetLanguageString(json, "cheeseMaturationPeriodDescription");
                dairy.FatPercentageInDryMaterial = GetDoubleValue(json, "fatpercentageInDryMatter");
                dairy.FatContentInMilk = GetDoubleValue(json, "fatInMilkContent");
                dairy.MaturationTime = GetMeasurement(json, "maturationTime");
            }
            return dairy;
        }
        private void SetDairy(JObject json, ITEProductDairy dairy)
        {
            if (json != null && dairy != null)
            {
                SetIDAsString(json, "isA2Dairy", dairy.IsA2);
                SetIDAsString(json, "isHomogenised", dairy.IsHomogenised);
                SetIDAsString(json, "isRindEdible", dairy.IsEdibleRind);
                SetIDAsString(json, "cheeseFirmness", dairy.CheeseFirmness);
                SetIDAsString(json, "sharpnessOfCheese", dairy.SharpnessOfCheese);
                SetLanguageString(json, "cheeseMaturationPeriodDescription", dairy.CheeseMaturationPeriodDescription);
                SetDouble(json, "fatpercentageInDryMatter", dairy.FatPercentageInDryMaterial);
                SetDouble(json, "fatInMilkContent", dairy.FatContentInMilk);
                SetMeasurement(json, "maturationTime", dairy.MaturationTime);
            }
        }

        private ITEProductAllergensAndAdditives GetAdditivesAndAllergens(JObject json)
        {
            ITEProductAllergensAndAdditives ana = new TEProductAllergensAndAdditives();
            if (json != null)
            {
                ana.AllergenSpecificationAgency = GetLanguageString(json, "allergenSpecificationAgency");
                ana.AllergenSpecificationName = GetLanguageString(json, "allergenSpecificationName");
                ana.AllergenSpecificationStatement = GetLanguageString(json, "allergenStatement");

                ana.Additives = new List<ITEProductAdditive>();
                if (json["additive"] is JArray)
                {
                    JArray jAdditives = json["additive"] as JArray;
                    foreach (JObject jAdd in jAdditives)
                    {
                        ITEProductAdditive add = new TEProductAdditive();
                        add.LevelOfContainment = LevelOfContainmentCode.GetFromKey(GetIDAsString(jAdd, "additiveLevelOfContainment"));
                        add.Name = GetLanguageString(jAdd, "additiveName");
                        ana.Additives.Add(add);
                    }
                }

                ana.AllergensList = new List<ITEProductAllergen>();
                if (json["hasAllergen"] is JArray)
                {
                    JArray jAllergens = json["hasAllergen"] as JArray;
                    foreach (JObject jAll in jAllergens)
                    {
                        ITEProductAllergen all = new TEProductAllergen();
                        all.LevelOfContainment = LevelOfContainmentCode.GetFromKey(GetIDAsString(jAll, "allergenLevelOfContainmentCode"));
                        all.Allergen = AllergenCode.GetFromKey(GetIDAsString(jAll, "allergenType"));
                        ana.AllergensList.Add(all);
                    }
                }
            }
            return ana;
        }
        private void SetAdditivesAndAllergens(JObject json, ITEProductAllergensAndAdditives ana)
        {
            if (json != null && ana != null)
            {
                SetLanguageString(json, "allergenSpecificationAgency", ana.AllergenSpecificationAgency);
                SetLanguageString(json, "allergenSpecificationName", ana.AllergenSpecificationName);
                SetLanguageString(json, "allergenStatement", ana.AllergenSpecificationStatement);

                if (ana.Additives != null)
                {
                    JArray jAdditives = new JArray();
                    foreach (ITEProductAdditive add in ana.Additives)
                    {
                        JObject jAdd = new JObject();
                        jAdd["@type"] = "gs1:AdditiveDetails";
                        SetIDAsString(jAdd, "additiveLevelOfContainment", add.LevelOfContainment);
                        SetLanguageString(jAdd, "additiveName", add.Name);
                        jAdditives.Add(jAdd);
                    }
                    json["additive"] = jAdditives;
                }

                if (ana.AllergensList != null)
                {
                    JArray jAllergens = new JArray();

                    foreach (ITEProductAllergen allergen in ana.AllergensList)
                    {
                        JObject jAll = new JObject();
                        jAll["@type"] = "gs1:AllergenDetails";
                        SetIDAsString(jAll, "allergenLevelOfContainmentCode", allergen.LevelOfContainment);
                        SetIDAsString(jAll, "allergenType", allergen.Allergen);
                        jAllergens.Add(jAll);
                    }

                    json["hasAllergen"] = jAllergens;
                }
            }
        }

        private ITEProductInfo GetInfo(JObject json)
        {
            ITEProductInfo info = new TEProductInfo();
            if (json != null)
            {
                info.ProductID = GetStringValue(json, "productID");
                info.ProductWebURI = GetUri(json, "@id");
                info.RegulatedProductName = GetStringValue(json, "regulatedProductName");
                info.ProductRange = GetStringValue(json, "productRange");
                info.Brand = GetStringValue((JObject)json["brand"], "brandName");
                info.SubBrand = GetStringValue((JObject)json["brand"], "subBrandName");
                info.BrandOwner = GetStringValue((JObject)json["brandOwner"], "@id");
                info.FunctionalName = GetLanguageString(json, "functionalName");
                info.GPCBrickValue = GetStringValue(json, "gpcCategoryCode");
                info.AdditionalProductDescription = GetStringValue(json, "additionalProductDescription");
                info.ProductMarketingMessage = GetStringValue(json, "productMarketingMessage");
                info.ProductFormDescription = GetStringValue(json, "productFormDescription");
                info.VariantDescription = GetStringValue(json, "variantDescription");
                info.ProductionVariantDescription = GetStringValue(json, "productionVariantDescription");
                info.ProductionVariantEffectiveDateTime = GetDateValue(json, "productionVariantEffectiveDateTime");
                info.ColourDescription = GetStringValue(json, "colourDescription");
                info.Manufacturer = GetStringValue((JObject)json["manufacturer"], "@id");
                info.ManufacturingPlant = GetStringValue((JObject)json["manufacturingPlant"], "@id");
                info.IsProductRecalled = (bool)GetBoolValue(json, "isProductRecalled");
                info.IncludedAccessories = GetStringValue(json, "includedAccessories");
                info.WarrantyDescription = GetStringValue((JObject)json["manufacturersWarranty"], "warrantyScopeDescription");
                info.WarrantyLength = GetMeasurement((JObject)json["manufacturersWarranty"], "durationOfWarranty");
            }
            return info;
        }
        private void SetInfo(JObject json, ITEProductInfo info)
        {
            if (json != null && info != null)
            {
                SetIDAsString(json, "brandOwner", info.BrandOwner);
                SetString(json, "gpcCategoryCode", info.GPCBrickValue);
                SetLanguageString(json, "additionalProductDescription", info.AdditionalProductDescription);
                SetLanguageString(json, "productMarketingMessage", info.ProductMarketingMessage);
                SetLanguageString(json, "productFormDescription", info.ProductFormDescription);
                SetLanguageString(json, "variantDescription", info.VariantDescription);
                SetLanguageString(json, "productionVariantDescription", info.ProductionVariantDescription);
                SetDateTimeAsObject(json, "productionVariantEffectiveDateTime", info.ProductionVariantEffectiveDateTime);
                SetLanguageString(json, "colourDescription", info.ColourDescription);
                //SetLanguageString(json, "gpcCategoryDescription", info.gpcCategoryDescription); Property not implemented yet
                //SetString(json, "gpcCategoryCode", info.gpcCategoryCode); Property not implemented yet
                SetIDAsString(json, "manufacturer", info.Manufacturer);
                SetIDAsString(json, "manufacturingPlant", info.ManufacturingPlant);
                SetBoolAsObject(json, "isProductRecalled", info.IsProductRecalled);
                SetString(json, "productID", info.ProductID);
                SetLanguageString(json, "includedAccessories", info.IncludedAccessories);
                SetString(json, "@id", info.ProductWebURI);
                SetLanguageString(json, "regulatedProductName", info.RegulatedProductName);
                SetLanguageString(json, "productRange", info.ProductRange);
                SetLanguageString(json, "functionalName", info.FunctionalName);

                // warranty
                JObject jWarranty = new JObject();
                jWarranty["@type"] = "gs1:WarrantyPromise";
                SetMeasurement(jWarranty, "durationOfWarranty", info.WarrantyLength);
                SetString(jWarranty, "warrantyScopeDescription", info.WarrantyDescription);
                json["manufacturersWarranty"] = jWarranty;
            }
        }

        private ITEProductMeat GetMeat(JObject json)
        {
            ITEProductMeat meat = new TEProductMeat();
            if (json != null)
            {
                meat.AnatomicalForm = AnatomicalFormCode.GetFromKey(GetIDAsString(json, "anatomicalForm"));
                meat.BonelessClaim = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "bonelessClaim"));
                meat.TypeOfMeatAndPoultry = GetLanguageString(json, "meatPoultryType");
                meat.MinimumMeatContent = GetMeasurement(json, "minimumMeatPoultryContent");
            }
            return meat;
        }
        private void SetMeat(JObject json, ITEProductMeat meat)
        {
            if (json != null && meat != null)
            {
                SetIDAsString(json, "anatomicalForm", meat.AnatomicalForm);
                SetIDAsString(json, "bonelessClaim", meat.BonelessClaim);
                SetLanguageString(json, "meatPoultryType", meat.TypeOfMeatAndPoultry);
                SetMeasurement(json, "minimumMeatPoultryContent", meat.MinimumMeatContent);
            }
        }

        ITEProductFruitsAndVegetables GetFruitsAndVegetables(JObject json)
        {
            ITEProductFruitsAndVegetables fruits = new TEProductFruitsAndVegetables();
            if (json != null)
            {
                fruits.IsPittedOrStoned = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isPittedStoned"));
                fruits.IsSeedless = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isSeedless"));
                fruits.IsShelledOrPeeled = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isShelledPeeled"));
                fruits.IsWashedAndReadyToEat = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isWashedReadyToEat"));
                fruits.MaturationMethod = MaturationMethodCode.GetFromKey(GetIDAsString(json, "maturationMethod"));
            }
            return fruits;
        }
        private void SetFruitsAndVegetables(JObject json, ITEProductFruitsAndVegetables fruits)
        {
            if (json != null && fruits != null)
            {
                SetIDAsString(json, "isPittedStoned", fruits.IsPittedOrStoned);
                SetIDAsString(json, "isSeedless", fruits.IsSeedless);
                SetIDAsString(json, "isShelledPeeled", fruits.IsShelledOrPeeled);
                SetIDAsString(json, "isWashedReadyToEat", fruits.IsWashedAndReadyToEat);
                SetIDAsString(json, "maturationMethod", fruits.MaturationMethod);
            }
        }

        private ITEProductBeverage GetBeverage(JObject json)
        {
            ITEProductBeverage beverage = new TEProductBeverage();
            if (json != null)
            {
                beverage.AlcoholicBeverageSubRegion = GetLanguageString(json, "alcoholicBeverageSubregion");
                beverage.BeverageVintage = GetStringValue(json, "beverageVintage");
                beverage.IsVintage = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isVintage"));
                beverage.Vintner = GetLanguageString(json, "vintner");
                beverage.PercentageOfAlcohol = GetDoubleValue(json, "percentageOfAlcoholByVolume");
                beverage.IsFromConcentrate = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isFromConcentrate"));
                beverage.IsDecaffeinated = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isDecaffeinated"));
                beverage.IsCarbonated = NonBinaryLogicCode.GetFromKey(GetIDAsString(json, "isCarbonated"));
            }
            return beverage;
        }
        private void SetBeverage(JObject json, ITEProductBeverage beverage)
        {
            if (json != null && beverage != null)
            {
                SetLanguageString(json, "alcoholicBeverageSubregion", beverage.AlcoholicBeverageSubRegion);
                SetString(json, "beverageVintage", beverage.BeverageVintage);
                SetIDAsString(json, "isVintage", beverage.IsVintage);
                SetLanguageString(json, "vintner", beverage.Vintner);
                SetDouble(json, "percentageOfAlcoholByVolume", beverage.PercentageOfAlcohol);
                SetIDAsString(json, "isFromConcentrate", beverage.IsFromConcentrate);
                SetIDAsString(json, "isDecaffeinated", beverage.IsDecaffeinated);
                SetIDAsString(json, "isCarbonated", beverage.IsCarbonated);
            }
        }

        private ITEProductSeafood GetSeafood(JObject json, string propertyName)
        {
            ITEProductSeafood seafood = new TEProductSeafood();
            if (json?[propertyName] is JObject)
            {
                JObject jSeafood = json[propertyName] as JObject;
                seafood.FishingMethod = FishingMethod.GetFromKey(GetStringValue(jSeafood, "fishingMethod"));
                seafood.ProductForm = ProductForm.GetFromKey(GetStringValue(jSeafood, "productForm"));
                seafood.ProductCondition = ProductCondition.GetFromKey(GetStringValue(jSeafood, "productCondition"));
                seafood.ProductionMethod = ProductionMethod.GetFromKey(GetStringValue(jSeafood, "productionMethod"));
                seafood.RearingMethod = RearingMethod.GetFromKey(GetStringValue(jSeafood, "rearingMethod"));
            }
            return seafood;
        }
        private void SetSeafood(JObject json, string propertyName, ITEProductSeafood seafood)
        {
            if (json != null && seafood != null)
            {
                JObject jSeafood = new JObject();
                jSeafood["@type"] = "gs1:seafood";

                SetString(jSeafood, "fishingMethod", seafood.FishingMethod);
                SetString(jSeafood, "productCondition", seafood.ProductCondition);
                SetString(jSeafood, "productForm", seafood.ProductForm);
                SetString(jSeafood, "productionMethod", seafood.ProductionMethod);
                SetString(jSeafood, "rearingMethod", seafood.RearingMethod);

                json[propertyName] = jSeafood;
            }
        }

        private List<Species> GetSpecies(JObject json, string propertyName)
        {
            List<Species> species = new List<Species>();
            if(json?[propertyName] != null && json[propertyName] is JArray)
            {
                JArray jArr = json[propertyName] as JArray;
                foreach (JObject jSpecies in jArr)
                {
                    string alpha3Code = GetStringValue(jSpecies, "alpha3code");
                    Species spec = SpeciesList.GetSpeciesByAlphaCode(alpha3Code);
                    if (spec != null)
                    {
                        species.Add(spec);
                    }
                }
            }
            return species;
        }
        private void SetSpecies(JObject json, string propertyName, List<Species> species)
        {
            if (json != null && species != null)
            {
                JArray jSpecies = new JArray();

                foreach (Species spec in species)
                {
                    JObject jSpec = new JObject();
                    jSpec["@type"] = "gs1:species";
                    SetString(jSpec, "alpha3code", spec.Alpha3Code);
                    jSpecies.Add(jSpec);
                }

                json[propertyName] = jSpecies;
            }
        }

        private List<ITEProductCase> GetCases(JObject json, string propertyName)
        {
            List<ITEProductCase> cases = new List<ITEProductCase>();
            if (json?[propertyName] != null && json[propertyName] is JArray)
            {
                JArray jArr = json[propertyName] as JArray;
                foreach (JObject jCase in jArr)
                {
                    ITEProductCase productCase = new TEProductCase();
                    productCase.CaseGTIN = IdentifierFactory.ParseGTIN(GetStringValue(jCase, "gtin"));
                    productCase.CaseName = GetLanguageString(jCase, "name");
                    productCase.Height = GetMeasurement(jCase, "height");
                    productCase.Width = GetMeasurement(jCase, "width");
                    productCase.Length = GetMeasurement(jCase, "length");
                    productCase.NetWeight = GetMeasurement(jCase, "netWeight");
                    productCase.InnerProductCount = GetIntValue(jCase, "innerProductCount");
                    cases.Add(productCase);
                }
            }
            return cases;
        }
        private void SetCases(JObject json, string propertyName, List<ITEProductCase> cases)
        {
            if (json != null && cases != null)
            {
                JArray jCases = new JArray();

                foreach (ITEProductCase productCase in cases)
                {
                    JObject jCase = new JObject();

                    SetString(jCase, "gtin", productCase.CaseGTIN?.ToString());
                    SetLanguageString(jCase, "name", productCase.CaseName);
                    SetMeasurement(jCase, "height", productCase.Height);
                    SetMeasurement(jCase, "width", productCase.Width);
                    SetMeasurement(jCase, "length", productCase.Length);
                    SetMeasurement(jCase, "netWeight", productCase.NetWeight);
                    SetInteger(jCase, "innerProductCount", productCase.InnerProductCount);

                    jCases.Add(jCase);
                }

                json[propertyName] = jCases;
            }
        }

        private void SetAdditionalProductClassification(JObject json, ITEProduct product)
        {
            if (product.Info.AdditionalClassificationSchemes != null)
            {
                JArray jSchemes = new JArray();
                foreach (TEAdditionalProductClassificationSchema scheme in product.Info.AdditionalClassificationSchemes)
                {
                    JObject jScheme = new JObject();
                    SetString(jScheme, "additionalProductClassificationCode", scheme.ClassificationScheme);
                    SetString(jScheme, "additionalProductClassificationValue", scheme.ClassificationCodeValue);
                    SetLanguageString(jScheme, "additionalProductClassificationDescription", scheme.ClassificationCodeDescription);
                    SetString(jScheme, "@type", "gs1:AdditionalProductClassificationDetails");
                    jSchemes.Add(jScheme);
                }
                json["additionalProductClassification"] = jSchemes;
            }
        }
        private void GetAdditionalProductClassification(JObject json, ITEProduct product)
        {
            if (json["additionalProductClassification"] != null)
            {
                product.Info.AdditionalClassificationSchemes = new List<ITEAdditionalProductClassificationSchema>();
                foreach (JObject jObject in json["additionalProductClassification"])
                {
                    ITEAdditionalProductClassificationSchema schema = new TEAdditionalProductClassificationSchema();
                    schema.ClassificationScheme = GetStringValue(jObject, "additionalProductClassificationCode");
                    schema.ClassificationCodeValue = GetStringValue(jObject, "additionalProductClassificationValue");
                    schema.ClassificationCodeDescription = GetStringValue(jObject, "additionalProductClassificationDescription");
                    product.Info.AdditionalClassificationSchemes.Add(schema);
                }
            }
        }

        private void SetBrand(JObject json, ITEProduct product)
        {
            if (product.Info.Brand != null)
            {
                json["brand"] = new JObject();
                SetLanguageString((JObject)json["brand"], "brandName", product.Info.Brand);
                if (product.Info.SubBrand != null)
                {
                    SetLanguageString((JObject)json["brand"], "subBrandName", product.Info.SubBrand);
                }
                SetString((JObject)json["brand"], "@type", "gs1:Brand");
            }
            else if (product.Info.SubBrand != null)
            {
                json["brand"] = new JObject();
                SetLanguageString((JObject)json["brand"], "subBrandName", product.Info.SubBrand);
                SetString((JObject)json["brand"], "@type", "gs1:Brand");
            }
        }

        private void SetColourCode(JObject json, ITEProduct product)
        {
            if (product.Info.ColourCodes != null)
            {
                JArray jColours = new JArray();
                foreach (TEProductColour colour in product.Info.ColourCodes)
                {
                    JObject jColour = new JObject();
                    SetString(jColour, "colourCodeValue", colour.Value);
                    SetIDAsString(jColour, "colourCodeList", colour.ColourCode);
                    SetString(jColour, "@type", "gs1:ColourCodeDetails");
                    jColours.Add(jColour);
                }
                json["colourCode"] = jColours;
            }
        }
        private void GetColourCode(JObject json, ITEProduct product)
        {
            if (json["colourCode"] != null)
            {
                product.Info.ColourCodes = new List<ITEProductColour>();
                foreach (JObject jObject in json["colourCode"])
                {
                    TEProductColour colour = new TEProductColour();
                    colour.ColourCode = ColourCode.GetFromKey(GetStringValue((JObject)jObject["colourCodeList"], "@id"));
                    colour.Value = GetStringValue(jObject, "colourCodeValue");
                    product.Info.ColourCodes.Add(colour);
                }
            }
        }

        private void SetConsumerSalesConditions(JObject json, ITEProduct product)
        {
            if (product.Info.ConsumerSalesConditionsCodes != null)
            {
                JArray jCodes = new JArray();
                foreach (TraceabilityEngine.StaticData.ConsumerSalesConditionsCode code in product.Info.ConsumerSalesConditionsCodes)
                {
                    JObject jCode = new JObject();
                    jCode["@id"] = code.Key;
                    jCodes.Add(jCode);
                }
                json["consumerSalesCondition"] = jCodes;
            }
        }
        private void GetConsumerSalesConditions(JObject json, ITEProduct product)
        {
            if (json["consumerSalesCondition"] != null)
            {
                product.Info.ConsumerSalesConditionsCodes = new List<ConsumerSalesConditionsCode>();
                foreach (JObject jObject in json["consumerSalesCondition"])
                {
                    ConsumerSalesConditionsCode code = new ConsumerSalesConditionsCode();
                    code = ConsumerSalesConditionsCode.GetFromKey(GetStringValue(jObject, "@id"));
                    product.Info.ConsumerSalesConditionsCodes.Add(code);
                }
            }
        }

        private void SetNutrientBasisQuantity(JObject json, TEMeasurement measurement)
        {
            if (measurement != null && measurement.Value != 0)
            {
                json["nutrientBasisQuantity"] = new JObject();
                json["nutrientBasisQuantity"]["value"] = measurement.Value;
                json["nutrientBasisQuantity"]["unitCode"] = measurement.UoM.UNCode;
                json["nutrientBasisQuantity"]["@type"] = "gs1:QuantitativeValue";
            }
        }
        private void SetPerNutrientBasis(JObject json, string nutrient, ITEProductNutritionWeightMeasurement measurement)
        {
            if (!ITEProductNutritionWeightMeasurement.IsNullOrEmpty(measurement)) //Not sure why this evaluates true when measurement has value but w/e
            {
                json[nutrient] = new JObject();
                if (!TEMeasurement.IsNullOrEmpty(measurement.Weight) && measurement.Weight.Value > 0)
                {
                    SetNullDouble((JObject)json[nutrient], "value", measurement.Weight.Value);
                    SetString((JObject)json[nutrient], "unitCode", measurement.Weight.UoM.UNCode);
                }
                SetNullDouble((JObject)json[nutrient], "dailyValueIntakePercent", measurement.DailyPercent); //ALWAYS NULL???
                SetString((JObject)json[nutrient], "@type", "gs1:NutritionMeasurementType");
            }
        }
        private void SetEnergyPerNutrientBasis(JObject json, string nutrient, ITEProductNutrition nutrition)
        {
            if (!ITEProductNutritionCalorieMeasurement.IsNullOrEmpty(nutrition?.EnergyCalories)
             || !ITEProductNutritionJoulesMeasurement.IsNullOrEmpty(nutrition?.EnergyJoules))
            {
                JArray jArray = new JArray();
                if (!ITEProductNutritionCalorieMeasurement.IsNullOrEmpty(nutrition?.EnergyCalories))
                {
                    JObject jItem = new JObject();
                    SetNullDouble(jItem, "value", nutrition.EnergyCalories.Calories);
                    SetString(jItem, "unitCode", "D70");
                    SetNullDouble(jItem, "dailyValueIntakePercent", nutrition.EnergyCalories.DailyPercent); //ALWAYS NULL???
                    SetString(jItem, "@type", "gs1:NutritionMeasurementType");
                    jArray.Add(jItem);
                }
                if (!ITEProductNutritionJoulesMeasurement.IsNullOrEmpty(nutrition?.EnergyJoules))
                {
                    JObject jItem = new JObject();
                    SetNullDouble(jItem, "value", nutrition.EnergyJoules.Joules);
                    SetString(jItem, "unitCode", "JOU");
                    SetNullDouble(jItem, "dailyValueIntakePercent", nutrition.EnergyJoules.DailyPercent); //ALWAYS NULL???
                    SetString(jItem, "@type", "gs1:NutritionMeasurementType");
                    jArray.Add(jItem);
                }
                json[nutrient] = jArray;
            }
        }
        private void SetEnergyFatPerNutrientBasis(JObject json, string nutrient, ITEProductNutrition nutrition)
        {
            if (!ITEProductNutritionCalorieMeasurement.IsNullOrEmpty(nutrition?.EnergyFromFatCalories)
            || !ITEProductNutritionJoulesMeasurement.IsNullOrEmpty(nutrition?.EnergyFromFatJoules))
            {
                JArray jArray = new JArray();
                if (!ITEProductNutritionCalorieMeasurement.IsNullOrEmpty(nutrition?.EnergyFromFatCalories))
                {
                    JObject jItem = new JObject();
                    SetNullDouble(jItem, "value", nutrition.EnergyFromFatCalories.Calories);
                    SetString(jItem, "unitCode", "D70");
                    SetNullDouble(jItem, "dailyValueIntakePercent", nutrition.EnergyFromFatCalories.DailyPercent); //ALWAYS NULL???
                    SetString(jItem, "@type", "gs1:NutritionMeasurementType");
                    jArray.Add(jItem);
                }
                if (!ITEProductNutritionJoulesMeasurement.IsNullOrEmpty(nutrition?.EnergyFromFatJoules))
                {
                    JObject jItem = new JObject();
                    SetNullDouble(jItem, "value", nutrition.EnergyFromFatJoules.Joules);
                    SetString(jItem, "unitCode", "JOU");
                    SetNullDouble(jItem, "dailyValueIntakePercent", nutrition.EnergyFromFatJoules.DailyPercent); //ALWAYS NULL???
                    SetString(jItem, "@type", "gs1:NutritionMeasurementType");
                    jArray.Add(jItem);
                }
                json[nutrient] = jArray;
            }
        }
        private void SetDietCode(JObject json, ITEProduct product)
        {
            if (product.Ingredients.DietTypes != null)
            {
                JArray jDietCodes = new JArray();
                foreach (TEDietType diet in product.Ingredients.DietTypes)
                {
                    JObject jDietCode = new JObject();
                    SetIDAsString(jDietCode, "dietType", diet.DietTypeCode);
                    SetLanguageString(jDietCode, "dietTypeSubcode", diet.DietTypeSubCode);
                    SetString(jDietCode, "@type", "gs1:DietTypeCodeDetails");
                    jDietCodes.Add(jDietCode);
                }
                json["dietCode"] = jDietCodes;
            }
        }

        private void SetIntItem(JObject json, string propertyName, int value) //Move to base class?
        {
            json[propertyName] = new JObject();
            json[propertyName]["@value"] = value.ToString();
            json[propertyName]["@type"] = "xsd:integer";
        }

        private void SetLongItem(JObject json, string propertyName, long value) //Move to base class?
        {
            json[propertyName] = new JObject();
            json[propertyName]["@value"] = value.ToString();
            json[propertyName]["@type"] = "xsd:integer";
        }

        private TEProductNutritionWeightMeasurement GetNutrient(JObject json, string propertyName)
        {
            TEProductNutritionWeightMeasurement nutrient = new TEProductNutritionWeightMeasurement()
            {
                Weight = GetMeasurement(json, propertyName),
                DailyPercent = GetDoubleValue((JObject)json[propertyName], "dailyValueIntakePercent")
            };
            return nutrient;
        }

        private void SetPackaging(JObject json, ITEProduct product) //Check how well this works with multiple values
        {
            if (product.Packaging != null)
            {
                json["packaging"] = new JObject();
                SetIDAsString((JObject)json["packaging"], "packagingShape", product.Packaging.PackageShape);
                foreach (PackageRecyclingSchema schema in product.Packaging.PackageRecyclingSchemas)
                {
                    SetIDAsArray((JObject)json["packaging"], "packagingRecyclingScheme", schema.Key);
                }
                foreach (PackageRecyclingProcessType process in product.Packaging.PackageRecyclingProcessTypes)
                {
                    SetIDAsArray((JObject)json["packaging"], "packagingRecyclingProcessType", process.Key);
                }
                foreach (PackageFunction function in product.Packaging.PackageFunctions)
                {
                    SetIDAsArray((JObject)json["packaging"], "packagingFunction", function.Key);
                }
                foreach (PackagingFeature feature in product.Packaging.PackagingFeatures)
                {
                    SetIDAsArray((JObject)json["packaging"], "packagingFeature", feature.Key);
                }
                SetString((JObject)json["packaging"], "packagingType", product.Packaging.PackageType);
                SetString((JObject)json["packaging"], "packMediumKey", product.Measurements.PackMediumKey);
                SetString((JObject)json["packaging"], "@type", "gs1:PackagingDetails");
            }
        }
        private void GetPackaging(JObject json, ITEProduct product)
        {
            if (json?["packaging"] != null)
            {
                product.Packaging.PackageShape = PackageShape.GetFromKey(GetStringValue((JObject)json["packaging"]["packagingShape"], "@id"));

                product.Packaging.PackageRecyclingSchemas = new List<PackageRecyclingSchema>();
                if (json["packaging"]["packagingRecyclingScheme"] is JArray)
                {
                    foreach (JObject jObject in json["packaging"]["packagingRecyclingScheme"])
                    {
                        PackageRecyclingSchema schema = new PackageRecyclingSchema();
                        schema = PackageRecyclingSchema.GetFromKey(GetStringValue(jObject, "@id"));
                        product.Packaging.PackageRecyclingSchemas.Add(schema);
                    }
                }

                product.Packaging.PackageRecyclingProcessTypes = new List<PackageRecyclingProcessType>();
                if (json["packaging"]["packagingRecyclingProcessType"] is JArray)
                {
                    foreach (JObject jObject in json["packaging"]["packagingRecyclingProcessType"])
                    {
                        PackageRecyclingProcessType type = new PackageRecyclingProcessType();
                        type = PackageRecyclingProcessType.GetFromKey(GetStringValue(jObject, "@id"));
                        product.Packaging.PackageRecyclingProcessTypes.Add(type);
                    }
                }

                product.Packaging.PackageFunctions = new List<PackageFunction>();
                if (json["packaging"]["packagingFunction"] is JArray)
                {
                    foreach (JObject jObject in json["packaging"]["packagingFunction"])
                    {
                        PackageFunction function = new PackageFunction();
                        function = PackageFunction.GetFromKey(GetStringValue(jObject, "@id"));
                        product.Packaging.PackageFunctions.Add(function);
                    }
                }

                product.Packaging.PackagingFeatures = new List<PackagingFeature>();
                if (json["packaging"]["packagingFeature"] is JArray)
                {
                    foreach (JObject jObject in json["packaging"]["packagingFeature"])
                    {
                        PackagingFeature feature = new PackagingFeature();
                        feature = PackagingFeature.GetFromKey(GetStringValue(jObject, "@id"));
                        product.Packaging.PackagingFeatures.Add(feature);
                    }
                }

                product.Measurements.PackMediumKey = GetStringValue((JObject)json["packaging"], "packMediumKey");
                product.Packaging.PackageType = PackageType.GetFromKey(GetStringValue((JObject)json["packaging"], "packagingType"));
            }
        }
        private void SetPackagingMaterial(JObject json, ITEProduct product)
        {
            JArray jPackagingMaterials = new JArray();
            JObject jPackingMaterial = new JObject();
            //SetQuantitativeValue(jPackingMaterial, "packagingMaterialCompositionQuantity", product.Packaging.Material.Quantity); TEPRODUCT.PACKAGING.MATERIAL NOT IMPLEMENTED YET
        }
        private List<ITEProductYield> GetProductYields(JObject json)
        {
            List<ITEProductYield> yields = new List<ITEProductYield>();
            if (json["yield"] is JArray)
            {
                foreach (JObject item in json["yield"])
                {
                    ITEProductYield yield = new TEProductYield();
                    yield.ProductYieldType = ProductYieldTypeCode.GetFromKey(GetIDAsString(item, "productYieldType"));
                    yield.VariationPercent = GetDoubleValue(item, "productYieldVariationPercentage");
                    yield.Amount = GetMeasurement(item, "productYield");
                    yields.Add(yield);
                }
            }
            return yields;
        }
        private void SetProductAvailabilities(JObject json, List<ITEProductAvailability> avails)
        {
            if (json != null && avails != null)
            {
                JArray jAvails = new JArray();
                foreach (TEProductAvailability avail in avails)
                {
                    JObject jAvail = new JObject();
                    jAvail["gs1:productAvailability"] = new JObject(jAvail);
                    SetString((JObject)jAvail["gs1:productAvailability"], "gs1:globalLocationNumber", avail.GLN?.ToString());
                    SetDouble((JObject)jAvail["gs1:productAvailability"], "gs1:price", avail.Price);
                    jAvails.Add(jAvail);
                }
                json["gs1:productAvailabilities"] = jAvails;
            }
        }
        private void SetProductTreatments(JObject json, List<ITEProductTreatment> treatments)
        {
            if (json != null && treatments != null)
            {
                JArray jTreatments = new JArray();
                foreach (TEProductTreatment treatment in treatments)
                {
                    JObject jTreatment = new JObject();
                    SetString(jTreatment, "gs1:foodTreatment", treatment.TreatmentType?.Key);
                    SetMeasurement(jTreatment, "gs1:treatmentTime", treatment.Time);
                    SetMeasurement(jTreatment, "gs1:treatmentConcentration", treatment.Concentration);
                    SetMeasurement(jTreatment, "gs1:treatmentTemperature", treatment.Temperature);
                    jTreatments.Add(jTreatment);
                }
                json["gs1:treatments"] = jTreatments;
            }
        }
        private void SetReferencedFileDetails(JObject json, string propertyName, string value)
        {
            if (value != null)
            {
                json[propertyName] = new JObject();
                SetIDAsString((JObject)json[propertyName], "referencedFileURL", value);
                SetString((JObject)json[propertyName], "@type", "ReferencedFileDetails");
            }
        }
        private string GetReferencedFileDetails(JObject json, string propertyName)
        {
            string value = GetIDAsString((JObject)json[propertyName], "referencedFileURL");
            return value;
        }
        private void SetSizeCode(JObject json, ITEProduct product)
        {
            if (product.Measurements.SizeCodeListCode != null || product.Measurements.SizeCodeValue != null)
            {
                JArray jCodes = new JArray();
                JObject jCode = new JObject();
                if (product.Measurements.SizeCodeListCode != null)
                {
                    SetString(jCode, "sizeCodeListCode", product.Measurements.SizeCodeListCode);
                }
                if (product.Measurements.SizeCodeValue != null)
                {
                    SetString(jCode, "sizeCodeValue", product.Measurements.SizeCodeValue);
                }
                SetString(jCode, "@type", "gs1:SizeCodeDetails");
                jCodes.Add(jCode);
                json["sizeCode"] = jCodes;
            }
        }
        private void GetSizeCode(JObject json, ITEProduct product)
        {
            if (json["sizeCode"] != null)
            {
                foreach (JObject jObject in json["sizeCode"])
                {
                    product.Measurements.SizeCodeListCode = GetStringValue(jObject, "sizeCodeListCode");
                    product.Measurements.SizeCodeValue = GetStringValue(jObject, "sizeCodeValue");
                }
            }

        }
        private void SetTargetMarket(JObject json, ITEProduct product)
        {

        }
    }
}

