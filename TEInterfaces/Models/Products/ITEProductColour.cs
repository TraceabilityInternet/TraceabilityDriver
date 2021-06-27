using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductColour
    {
        ColourCode ColourCode { get; set; }
        string Value { get; set; }
    }
}
