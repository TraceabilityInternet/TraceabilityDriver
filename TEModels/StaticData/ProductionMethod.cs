using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class ProductionMethod : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductionMethod GetFromKey(string key)
        {
            return GetProductionMethods().Find(c => c.Key == key);
        }

        public static List<ProductionMethod> GetProductionMethods()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.ProductionMethods.xml");
            List<ProductionMethod> productionMethods = new List<ProductionMethod>();
            foreach  (DSXML xProductionMethod in xml.ChildElements)
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
