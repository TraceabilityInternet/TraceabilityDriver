using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductOrganicClaim
    {
        OrganicLabel OrganicLabel { get; set; }
        double OrganicPercentage { get; set; }
    }
}
