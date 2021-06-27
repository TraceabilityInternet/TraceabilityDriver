using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Models;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Models.Products
{
    [DataContract]
    public class TEProduct : TEProductLite, ITEProduct
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ObjectID { get; set; }

        [DataMember]
        public string InternalID { get; set; }

        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public ProductTypeCode ProductType { get; set; }

        [DataMember]
        public ITEProductInfo Info { get; set; } = new TEProductInfo();

        [DataMember]
        public ITEProductSeafood Seafood { get; set; } = new TEProductSeafood();

        [DataMember]
        public List<Species> Species { get; set; } = new List<Species>();

        [DataMember]
        public List<ProductClaim> Claims { get; set; } = new List<ProductClaim>();

        [DataMember]
        public List<GS1Category> Categories { get; set; } = new List<GS1Category>();

        [DataMember]
        public ITEProductMeasurements Measurements { get; set; } = new TEProductMeasurements();

        [DataMember]
        public List<ITEProductCase> Cases { get; set; } = new List<ITEProductCase>();

        [DataMember]
        public List<ITEProductChild> Children { get; set; } = new List<ITEProductChild>();

        [DataMember]
        public List<ITEProductTreatment> Treatments { get; set; } = new List<ITEProductTreatment>();

        [DataMember]
        public ITEProductIngredients Ingredients { get; set; } = new TEProductIngredients();

        [DataMember]
        public List<ITEProductAvailability> Availabilities { get; set; } = new List<ITEProductAvailability>();

        [DataMember]
        public List<ITEPhoto> Images { get; set; } = new List<ITEPhoto>();

        [DataMember]
        public ITEProductNutrition Nutrition { get; set; } = new TEProductNutrition();

        [DataMember]
        public List<ITEAttachment> Attachments { get; set; } = new List<ITEAttachment>();

        [DataMember]
        public ITEProductInstructions Instructions { get; set; } = new TEProductInstructions();

        [DataMember]
        public ITEProductPackaging Packaging { get; set; } = new TEProductPackaging();

        [DataMember]
        public ITEProductAllergensAndAdditives AllergensAndAdditives { get; set; } = new TEProductAllergensAndAdditives();

        [DataMember]
        public ITEProductPreparation Preparation { get; set; } = new TEProductPreparation();

        [DataMember]
        public ITEProductProvenance Provenance { get; set; } = new TEProductProvenance();

        [DataMember]
        public List<ITECertificate> Certificates { get; set; } = new List<ITECertificate>();

        [DataMember]
        public ITEProductBeverage Beverage { get; set; } = new TEProductBeverage();

        [DataMember]
        public ITEProductFruitsAndVegetables FruitsAndVegetables { get; set; } = new TEProductFruitsAndVegetables();

        [DataMember]
        public ITEProductDairy Dairy { get; set; } = new TEProductDairy();

        [DataMember]
        public ITEProductMeat Meat { get; set; } = new TEProductMeat();


        #region ITEJsonModel
        protected string JsonData { get; set; }

        public void FromJson(string jsonData)
        {
            throw new NotImplementedException();
        }

        public string ToJson()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
