using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductAdditive
    {
        LevelOfContainmentCode LevelOfContainment { get; set; }
        string Name { get; set; }
    }
}
