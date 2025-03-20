using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEAdditionalProductClassificationSchema
    {
        string ClassificationScheme { get; set; }
        string ClassificationCodeValue { get; set; }
        string ClassificationCodeDescription { get; set; }
    }
}
