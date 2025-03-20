using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class ProductCondition : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductCondition GetFromKey(string key)
        {
            return GetProductConditions().Find(c => c.Key == key);
        }

        public static List<ProductCondition> GetProductConditions()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.ProductConditions.xml");
            List<ProductCondition> productConditions = new List<ProductCondition>();
            foreach  (DSXML xProductCondition in xml.ChildElements)
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
