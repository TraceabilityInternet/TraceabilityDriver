using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEAdditionalProductClassificationSchema : ITEAdditionalProductClassificationSchema
    {
        public string ClassificationScheme { get; set; }
        public string ClassificationCodeValue { get; set; }
        public string ClassificationCodeDescription { get; set; }
    }
}
