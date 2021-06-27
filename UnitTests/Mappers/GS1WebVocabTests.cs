using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.Mappers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Models.Locations;
using TraceabilityEngine.Models;
using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.StaticData;
using System.Linq;
using TraceabilityEngine.Models.TradingParty;
using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Models.Products;

namespace UnitTests.Mappers
{
    [TestClass]
    public class GS1WebVocabTests
    {
        //[TestMethod]
        //public void Products()
        //{
        //    //List<IFDProduct> products = ModelHelpers.CreateProducts();
        //    //foreach(IFDProduct product in products)
        //    //{
        //    //    IFDProductMapper mapper = new ProductWebVocabMapper();
        //    //    string json = mapper.ConvertToJSON(product);
        //    //    IFDProduct afterProduct = mapper.ConvertToProduct(json);

        //    //    string beforeJSON = JsonConvert.SerializeObject(product);
        //    //    string afterJSON = JsonConvert.SerializeObject(afterProduct);

        //    //    Assert.AreEqual(beforeJSON, afterJSON);
        //    //}
        //}

        private List<ITELocation> GetLocations()
        {
            List<ITELocation> locations = new List<ITELocation>();

            ITELocation location = new TELocation();
            location.Address = new TEAddress()
            {
                Address1 = "15814 Blanco Trails Ln",
                Address2 = "Apartment #1",
                City = "Cypress",
                State = "TX",
                County = "Harris",
                ZipCode = "77429",
                Country = Countries.FromCountryIso(840)
            };
            location.Certificates = new List<TraceabilityEngine.Interfaces.Models.Common.ITECertificate>();
            location.Certificates.Add(new TECertificate()
            {
                CertificateType = CertificateType.GetList().First(),
                Agency = "Agency 123",
                AgencyURL = new Uri("https://www.google.com"),
                AuditDate = DateTime.Now,
                EndDate = DateTime.Now,
                Identification = "ID 123",
                URI = new Uri("urn:certificate:1234567890"),
                Standard = "Standard 123",
                StartDate = DateTime.Now,
                Subject = "Subject 123",
                Value = "Value 123",
                Attachment = new TEAttachment()
                {
                    FileName = "example.pdf",
                    Description = "attachment description",
                    AttachmentType = AttachmentType.GetList().First(),
                    PixelHeight = 100,
                    PixelWidth = 100,
                    Size = new TEMeasurement(123, "KGM"),
                    URI = new Uri("urn:attachment:123456890"),
                    URL = new Uri("https://www.google.com")
                }
            });
            location.Contacts = new List<TraceabilityEngine.Interfaces.Models.Common.ITEContact>();
            location.Contacts.Add(new TEContact()
            {
                ContactTitle = "General Manager",
                ContactType = "General Manager",
                Email = "philip.coding@gmail.com",
                Fax = "5125749250",
                Name = "Philip",
                Phone = "5125749250",
                Responsibility = "Doing stuff"
            });
            location.Description = "This is a processing plant.";
            location.Farm = new TEFarmInformation()
            {
                FarmNumber = "Farm 123",
                PermitNumber = "Permit 123"
            };
            location.GLN = IdentifierFactory.ParseGLN("urn:epc:id:sgln:08600031303.0");
            location.LocationWebURI = "https://www.google.com";
            location.Longitude = 12;
            location.Latitude = 12;
            location.Name = "Processing Plant";
            location.Photos = new List<TraceabilityEngine.Interfaces.Models.Common.ITEPhoto>();
            location.Photos.Add(new TEPhoto()
            {
                FileName = "example.png",
                PixelHeight = 123,
                PixelWidth = 123,
                URI = new Uri("https://www.google.com"),
                URL = new Uri("https://www.google.com")
            });
            location.Vessel = new TEVesselInformation()
            {
                IMONumber = "IM12345678",
                RMFONumber = "RMFO 123",
                VesselCallSign = "VC123",
                VesselFlag = Countries.FromCountryIso(840)
            };
            locations.Add(location);

            return locations;
        }
        private List<ITETradingParty> GetTradingParties()
        {
            List<ITETradingParty> tradingParties = new List<ITETradingParty>();

            ITETradingParty tp = new TETradingParty();
            tp.AdditionalOrganizationType = "123";
            tp.Address = new TEAddress()
            {
                Address1 = "15814 Blanco Trails Ln",
                Address2 = "Apartment #1",
                City = "Cypress",
                State = "TX",
                County = "Harris",
                ZipCode = "77429",
                Country = Countries.FromCountryIso(840)
            };
            tp.Certificates = new List<TraceabilityEngine.Interfaces.Models.Common.ITECertificate>();
            tp.Certificates.Add(new TECertificate()
            {
                CertificateType = CertificateType.GetList().First(),
                Agency = "Agency 123",
                AgencyURL = new Uri("https://www.google.com"),
                AuditDate = DateTime.Now,
                EndDate = DateTime.Now,
                Identification = "ID 123",
                URI = new Uri("urn:certificate:1234567890"),
                Standard = "Standard 123",
                StartDate = DateTime.Now,
                Subject = "Subject 123",
                Value = "Value 123",
                Attachment = new TEAttachment()
                {
                    FileName = "example.pdf",
                    Description = "attachment description",
                    AttachmentType = AttachmentType.GetList().First(),
                    PixelHeight = 100,
                    PixelWidth = 100,
                    Size = new TEMeasurement(123, "KGM"),
                    URI = new Uri("urn:attachment:123456890"),
                    URL = new Uri("https://www.google.com")
                }
            });
            tp.Contacts = new List<TraceabilityEngine.Interfaces.Models.Common.ITEContact>();
            tp.Contacts.Add(new TEContact()
            {
                ContactTitle = "General Manager",
                ContactType = "General Manager",
                Email = "philip.coding@gmail.com",
                Fax = "5125749250",
                Name = "Philip",
                Phone = "5125749250",
                Responsibility = "Doing stuff"
            });
            tp.Department = "123";
            tp.PGLN = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.0");
            tp.Logo = new List<ITEPhoto>();
            tp.Logo.Add(new TEPhoto()
            {
                FileName = "example.png",
                PixelHeight = 123,
                PixelWidth = 123,
                URI = new Uri("https://www.google.com"),
                URL = new Uri("https://www.google.com")
            });
            tp.Name = "some random trading partner";
            tp.OrganizationRole = OrganizationRole.GetList().FirstOrDefault();
            tp.OrganizationWebURI = new Uri("https://www.google.com");
            tradingParties.Add(tp);

            return tradingParties;
        }
        //private List<ITEProduct> GetProducts()
        //{
        //    List<ITEProduct> products = new List<ITEProduct>();

        //    ITEProduct product = new TEProduct();
        //    product.AllergensAndAdditives = new TEProductAllergensAndAdditives();
        //    product.AllergensAndAdditives.Additives = new List<ITEProductAdditive>();
        //    product.AllergensAndAdditives.Additives.Add(new TEProductAdditive()
        //    {
        //        LevelOfContainment = LevelOfContainmentCode.GetList().FirstOrDefault(),
        //        Name = "some_value_01"
        //    });
        //    product.AllergensAndAdditives.AllergensList = new List<ITEProductAllergen>();
        //    product.AllergensAndAdditives.AllergensList.Add(new TEProductAllergen()
        //    {
        //        Allergen = AllergenCode.GetList().FirstOrDefault(),
        //        LevelOfContainment = LevelOfContainmentCode.GetList().FirstOrDefault()
        //    });
        //    product.AllergensAndAdditives.AllergenSpecificationAgency = "some_value_02";
        //    product.AllergensAndAdditives.AllergenSpecificationName = "some_value_03";
        //    product.AllergensAndAdditives.AllergenSpecificationStatement = "some_value_04";
        //    product.Attachments = new List<ITEAttachment>();
        //    product.Attachments.Add(new TEAttachment()
        //    {
        //        FileName = "example.pdf",
        //        Description = "attachment description",
        //        AttachmentType = AttachmentType.GetList().First(),
        //        PixelHeight = 100,
        //        PixelWidth = 100,
        //        Size = new TEMeasurement(123, "KGM"),
        //        URI = new Uri("urn:attachment:123456890"),
        //        URL = new Uri("https://www.google.com")
        //    });
        //    product.Availabilities = new List<ITEProductAvailability>();
        //    product.Availabilities.Add(new TEProductAvailability()
        //    {
        //        GLN = IdentifierFactory.ParseGLN("0860003130308"),
        //        Price = 123.0
        //    });
        //    product.Beverage = new TEProductBeverage();
        //    product.Beverage.AlcoholicBeverageSubRegion = "some_value_05";
        //    product.Beverage.BeverageVintage = "some_value_06";
        //    product.Beverage.IsCarbonated = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.Beverage.IsDecaffeinated = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.Beverage.IsFromConcentrate = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.Beverage.IsVintage = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.Beverage.PercentageOfAlcohol = 0.5;
        //    product.Beverage.Vintner = "some_value_07";
        //    product.Cases = new List<ITEProductCase>();
        //    product.Cases.Add(new TEProductCase()
        //    {
        //        CaseGTIN = IdentifierFactory.ParseGTIN("10860003130305"),
        //        CaseName = "some_value_08",
        //        InnerProductCount = 1,
        //        Height = new TEMeasurement(1, "MTR"),
        //        Width = new TEMeasurement(1, "MTR"),
        //        NetWeight = new TEMeasurement(1, "KGM"),
        //        Length = new TEMeasurement(1, "MTR")
        //    });
        //    product.Categories = new List<GS1Category>();
        //    product.Categories.Add(GS1Category.GetList().FirstOrDefault());
        //    product.Category = "some_value_09";
        //    product.Certificates = new List<ITECertificate>();
        //    product.Certificates.Add(new TECertificate()
        //    {
        //        CertificateType = CertificateType.GetList().First(),
        //        Agency = "Agency 123",
        //        AgencyURL = new Uri("https://www.google.com"),
        //        AuditDate = DateTime.Now,
        //        EndDate = DateTime.Now,
        //        Identification = "ID 123",
        //        URI = new Uri("urn:certificate:1234567890"),
        //        Standard = "Standard 123",
        //        StartDate = DateTime.Now,
        //        Subject = "Subject 123",
        //        Value = "Value 123",
        //        Attachment = new TEAttachment()
        //        {
        //            FileName = "example.pdf",
        //            Description = "attachment description",
        //            AttachmentType = AttachmentType.GetList().First(),
        //            PixelHeight = 100,
        //            PixelWidth = 100,
        //            Size = new TEMeasurement(123, "KGM"),
        //            URI = new Uri("urn:attachment:123456890"),
        //            URL = new Uri("https://www.google.com")
        //        }
        //    });
        //    product.Children = new List<ITEProductChild>();
        //    product.Children.Add(new TEProductChild()
        //    {
        //        ChildGTIN = IdentifierFactory.ParseGTIN("00860003130308"),
        //        InnerProductCount = 3
        //    });
        //    product.Claims = new List<ProductClaim>();
        //    product.Claims.Add(ProductClaim.GetList().FirstOrDefault());
        //    product.Dairy = new TEProductDairy();
        //    product.Dairy.CheeseFirmness = CheeseFirmnessCode.GetList().FirstOrDefault();
        //    product.Dairy.CheeseMaturationPeriodDescription = "some_value_10";
        //    product.Dairy.FatContentInMilk = 0.5;
        //    product.Dairy.FatPercentageInDryMaterial = 0.5;
        //    product.Dairy.IsA2 = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.Dairy.IsEdibleRind = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.Dairy.IsHomogenised = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.Dairy.MaturationTime = new TEMeasurement(1, "KGM");
        //    product.Dairy.SharpnessOfCheese = SharpnessOfCheeseCode.GetList().FirstOrDefault();
        //    product.Description = "some_value_11";
        //    product.FruitsAndVegetables = new TEProductFruitsAndVegetables();
        //    product.FruitsAndVegetables.IsPittedOrStoned = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.FruitsAndVegetables.IsSeedless = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.FruitsAndVegetables.IsShelledOrPeeled = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.FruitsAndVegetables.IsWashedAndReadyToEat = NonBinaryLogicCode.GetList().FirstOrDefault();
        //    product.FruitsAndVegetables.MaturationMethod = MaturationMethodCode.GetList().FirstOrDefault();
        //    product.GTIN = IdentifierFactory.ParseGTIN("00860003130308");
        //    product.Images = new List<TraceabilityEngine.Interfaces.Models.Common.ITEPhoto>();
        //    product.Images.Add(new TEPhoto()
        //    {
        //        FileName = "example.png",
        //        PixelHeight = 123,
        //        PixelWidth = 123,
        //        URI = new Uri("https://www.google.com"),
        //        URL = new Uri("https://www.google.com")
        //    });
        //    product.Info = new TEProductInfo();
        //    product.Info.AdditionalClassificationSchemes = new List<ITEAdditionalProductClassificationSchema>();
        //    product.Info.AdditionalClassificationSchemes.Add(new TEAdditionalProductClassificationSchema()
        //    {
        //        ClassificationCodeDescription = "some_value_11",
        //        ClassificationCodeValue = "some_value_12",
        //        ClassificationScheme = "some_value_13"
        //    });
        //    product.Info.AdditionalProductDescription = "some_value_14";
        //    product.Info.Brand = "some_value_15";
        //    product.Info.BrandOwner = "some_value_16";
        //    product.Info.ColourCodes = new List<ITEProductColour>();
        //    product.Info.ColourCodes.Add(new TEProductColour() {
        //        ColourCode = ColourCode.GetList().FirstOrDefault(),
        //        Value = "some_value_17"
        //    });
        //    product.Info.ColourDescription = "some_value_18";
        //    product.Info.ConsumerSalesConditionsCodes = new List<ConsumerSalesConditionsCode>();
        //    product.Info.ConsumerSalesConditionsCodes.Add(ConsumerSalesConditionsCode.GetList().FirstOrDefault());
        //    product.Info.FunctionalName = "some_value_19";
        //    product.Info.GPCBrickValue = "some_value_20";
        //    product.Info.IncludedAccessories = "some_value_21";
        //    product.Info.IsProductRecalled = true;
        //    product.Info.Manufacturer = "some_value_21";
        //    product.Info.ManufacturingPlant = "some_value_22";
        //    product.Info.ProductFormDescription = "some_value_23";
        //    product.Info.ProductionVariantDescription = "some_value_24";
        //    product.Info.ProductionVariantEffectiveDateTime = DateTime.Now;
        //    product.Info.ProductMarketingMessage = "some_value_25";
        //    product.Info.ProductRange = "some_value_26";
        //    product.Info.ProductWebURI = new Uri("https://www.google.com");
        //    product.Info.RegulatedProductName = "some_value_27";
        //    product.Info.SubBrand = "some_value_28";
        //    product.Info.VariantDescription = "some_value_29";
        //    product.Info.WarrantyDescription = "some_value_30";
        //    product.Info.WarrantyLength = new TEMeasurement(1, "KGM");
        //    product.Ingredients = new TEProductIngredients();
        //    product.
        //}

        [TestMethod]
        public void Products()
        {
            // we need to make sure that the TEModels assembly loads for the reflective instantiating of interface classes
            ITEProduct p = new TEProduct();

            List<ITEProduct> products = ModelHelp.BuildData<ITEProduct>(1);
            foreach(ITEProduct prod in products)
            {
                ITEProductMapper mapper = new ProductWebVocabMapper();
                string json = mapper.ConvertFromProduct(prod);
                ITEProduct afterProduct = mapper.ConvertToProduct(json);

                string beforeJSON = JsonConvert.SerializeObject(prod, Formatting.Indented);
                string afterJSON = JsonConvert.SerializeObject(afterProduct, Formatting.Indented);

                Assert.AreEqual(beforeJSON, afterJSON);
            }
        }

        [TestMethod]
        public void Locations()
        {
            // we need to make sure that the TEModels assembly loads for the reflective instantiating of interface classes
            ITELocation loc = new TELocation();

            List<ITELocation> locations = ModelHelp.BuildData<ITELocation>(1);
            foreach (ITELocation location in locations)
            {
                location.IsPublic = false;
                location.Archived = false;
                location.ObjectID = null;

                ITELocationMapper mapper = new LocationWebVocabMapper();
                string json = mapper.ConvertFromLocation(location);
                ITELocation afterLocation = mapper.ConvertToLocation(json);

                string beforeJSON = JsonConvert.SerializeObject(location, Formatting.Indented);
                string afterJSON = JsonConvert.SerializeObject(afterLocation, Formatting.Indented);

                Assert.AreEqual(beforeJSON, afterJSON);
            }
        }

        [TestMethod]
        public void TradingParty()
        {
            // we need to make sure that the TEModels assembly loads for the reflective instantiating of interface classes
            ITETradingParty tp = new TETradingParty();

            List<ITETradingParty> tradingParties = ModelHelp.BuildData<ITETradingParty>(1);
            foreach (ITETradingParty tradingParty in tradingParties)
            {
                ITETradingPartyMapper mapper = new TradingPartyWebVocabMapper();
                string json = mapper.ConvertFromTradingParty(tradingParty);
                ITETradingParty afterTP = mapper.ConvertToTradingParty(json);

                string beforeJSON = JsonConvert.SerializeObject(tradingParty, Formatting.Indented);
                string afterJSON = JsonConvert.SerializeObject(afterTP, Formatting.Indented);

                Assert.AreEqual(beforeJSON, afterJSON);
            }
        }
    }
}
