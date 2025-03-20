using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductProvenance
    {
        List<Country> CountriesOfOrigin { get; set; }

        List<Country> CountriesOfProcessing { get; set; }

        List<Country> CountriesOfAssembly { get; set; }

        Country CountryOfLastProcessing { get; set; }

        string CountryOfOriginStatement { get; set; }

        string ProvenanceStatement { get; set; }

        string ProductFeatureBenefit { get; set; }

        string HealthClaimDescription { get; set; }

        string NutritionalClaimDescription { get; set; }

        List<NutritionalClaimCode> NutritionalClaims { get; set; }

        List<GrowingMethod> GrowingMethods { get; set; }

        NonBinaryLogicCode IsIrradiated { get; set; }

        NonBinaryLogicCode MayBeReheated { get; set; }

        LevelOfContainmentCode GMOContainmentStatus { get; set; }

        List<PackageAccreditationCode> PackageAccreditationCodes { get; set; }

        List<PackageFreeFromCode> PackageFreeFromCodes { get; set; }

        List<ITEProductOrganicClaim> OrganicClaims { get; set; }

        FoodBeverageRefrigerationClaimCode RefrigerationInformation { get; set; }
    }
}
