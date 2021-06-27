using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductProvenance : ITEProductProvenance
    {
        public List<Country> CountriesOfOrigin { get; set; } = new List<Country>();

        public List<Country> CountriesOfProcessing { get; set; } = new List<Country>();

        public List<Country> CountriesOfAssembly { get; set; } = new List<Country>();

        public Country CountryOfLastProcessing { get; set; }

        public string CountryOfOriginStatement { get; set; }

        public string ProvenanceStatement { get; set; }

        public string ProductFeatureBenefit { get; set; }

        public string HealthClaimDescription { get; set; }

        public string NutritionalClaimDescription { get; set; }

        public List<NutritionalClaimCode> NutritionalClaims { get; set; } = new List<NutritionalClaimCode>();

        public List<GrowingMethod> GrowingMethods { get; set; } = new List<GrowingMethod>();

        public NonBinaryLogicCode IsIrradiated { get; set; }

        public NonBinaryLogicCode MayBeReheated { get; set; }

        public LevelOfContainmentCode GMOContainmentStatus { get; set; }

        public List<PackageAccreditationCode> PackageAccreditationCodes { get; set; } = new List<PackageAccreditationCode>();

        public List<PackageFreeFromCode> PackageFreeFromCodes { get; set; } = new List<PackageFreeFromCode>();

        public List<ITEProductOrganicClaim> OrganicClaims { get; set; } = new List<ITEProductOrganicClaim>();

        public FoodBeverageRefrigerationClaimCode RefrigerationInformation { get; set; }
    }
}
