using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class ProductionMethod : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductionMethod GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<ProductionMethod> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.ProductionMethods.xml");
            List<ProductionMethod> productionMethods = new List<ProductionMethod>();
            foreach  (TEXML xProductionMethod in xml.ChildElements)
            {
                ProductionMethod productionMethod = new ProductionMethod();
                productionMethod.Key = xProductionMethod.Attribute("Key");
                productionMethod.Name = xProductionMethod.Attribute("Name");
                productionMethods.Add(productionMethod);
            }
            return productionMethods;
        }
    }
}
