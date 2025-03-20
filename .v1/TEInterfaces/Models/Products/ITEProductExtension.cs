using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductExtension
    {
        void MapToGS1WebVocabJSON(JObject jProduct)
        {

        }

        void MapFromGS1WebVocabJSON(JObject jProduct)
        {

        }
    }
}
