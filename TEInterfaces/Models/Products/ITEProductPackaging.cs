using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductPackaging
    {
        PackageType PackageType { get; set; }
        PackageShape PackageShape { get; set; }
        List<PackageRecyclingSchema> PackageRecyclingSchemas { get; set; }
        List<PackageRecyclingProcessType> PackageRecyclingProcessTypes { get; set; }
        List<PackageFunction> PackageFunctions { get; set; }
        List<PackagingFeature> PackagingFeatures { get; set; }
    }
}
