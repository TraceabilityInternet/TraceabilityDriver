using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductPackaging : ITEProductPackaging
    {
        public PackageType PackageType { get; set; }
        public PackageShape PackageShape { get; set; }
        public List<PackageRecyclingSchema> PackageRecyclingSchemas { get; set; } = new List<PackageRecyclingSchema>();
        public List<PackageRecyclingProcessType> PackageRecyclingProcessTypes { get; set; } = new List<PackageRecyclingProcessType>();
        public List<PackageFunction> PackageFunctions { get; set; } = new List<PackageFunction>();
        public List<PackagingFeature> PackagingFeatures { get; set; } = new List<PackagingFeature>();
    }
}
