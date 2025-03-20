using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductInfo
    {
        public string ProductID { get; set; }

        public string RegulatedProductName { get; set; }

        public string ProductRange { get; set; }

        public string ProductMarketingMessage { get; set; }

        public string BrandOwner { get; set; }

        public string Brand { get; set; }

        public string SubBrand { get; set; }

        public Uri ProductWebURI { get; set; }

        public string AdditionalProductDescription { get; set; }

        public string ProductFormDescription { get; set; }

        public string VariantDescription { get; set; }

        public string ProductionVariantDescription { get; set; }

        public DateTime? ProductionVariantEffectiveDateTime { get; set; }

        public string ColourDescription { get; set; }

        public List<ITEProductColour> ColourCodes { get; set; }

        public string FunctionalName { get; set; }

        public string GPCBrickValue { get; set; }

        public List<ITEAdditionalProductClassificationSchema> AdditionalClassificationSchemes { get; set; }

        public List<ConsumerSalesConditionsCode> ConsumerSalesConditionsCodes { get; set; }

        public string IncludedAccessories { get; set; }

        public string Manufacturer { get; set; }

        public string ManufacturingPlant { get; set; }

        public bool IsProductRecalled { get; set; }

        public TEMeasurement WarrantyLength { get; set; }

        public string WarrantyDescription { get; set; }
    }
}
