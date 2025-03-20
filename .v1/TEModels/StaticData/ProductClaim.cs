using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class ProductClaim : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductClaim GetFromKey(string key)
        {
            return GetProductClaims().Find(c => c.Key == key);
        }

        public static List<ProductClaim> GetProductClaims()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.ProductClaims.xml");
            List<ProductClaim> productClaims = new List<ProductClaim>();
            foreach  (DSXML xProductClaim in xml.ChildElements)
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
