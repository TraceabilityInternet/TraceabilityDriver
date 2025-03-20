using TraceabilityEngine.Interfaces.Models.Products;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Mappers
{
    public interface ITEProductMapper
    {
        ITEProduct ConvertToProduct(string strValue);
        string ConvertFromProduct(ITEProduct product);
    }
}
