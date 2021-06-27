using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class ProductClaim : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductClaim GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<ProductClaim> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.ProductClaims.xml");
            List<ProductClaim> productClaims = new List<ProductClaim>();
            foreach  (TEXML xProductClaim in xml.ChildElements)
            {
                ProductClaim productClaim = new ProductClaim();
                productClaim.Key = xProductClaim.Attribute("Key");
                productClaim.Name = xProductClaim.Attribute("Name");
                productClaims.Add(productClaim);
            }
            return productClaims;
        }
    }
}
