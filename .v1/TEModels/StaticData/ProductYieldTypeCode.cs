using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class ProductYieldTypeCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductYieldTypeCode GetFromKey(string key)
        {
            return GetProductYieldTypeCodes().Find(c => c.Key == key);
        }

        public static List<ProductYieldTypeCode> GetProductYieldTypeCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.ProductYieldTypeCodes.xml");
            List<ProductYieldTypeCode> codes = new List<ProductYieldTypeCode>();
            foreach  (DSXML xCode in xml.ChildElements)
            {
                ProductYieldTypeCode code = new ProductYieldTypeCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
