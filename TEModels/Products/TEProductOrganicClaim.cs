using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductOrganicClaim : ITEProductOrganicClaim
    {
        public OrganicLabel OrganicLabel { get; set; }
        public double OrganicPercentage { get; set; }
    }
}
