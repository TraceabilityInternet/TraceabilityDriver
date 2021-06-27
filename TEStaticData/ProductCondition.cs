using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class ProductCondition : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductCondition GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<ProductCondition> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.ProductConditions.xml");
            List<ProductCondition> productConditions = new List<ProductCondition>();
            foreach  (TEXML xProductCondition in xml.ChildElements)
            {
                ProductCondition productCondition = new ProductCondition();
                productCondition.Key = xProductCondition.Attribute("Key");
                productCondition.Name = xProductCondition.Attribute("Name");
                productConditions.Add(productCondition);
            }
            return productConditions;
        }
    }
}
