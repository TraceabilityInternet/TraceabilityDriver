using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class ProductYieldTypeCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductYieldTypeCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<ProductYieldTypeCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.ProductYieldTypeCodes.xml");
            List<ProductYieldTypeCode> codes = new List<ProductYieldTypeCode>();
            foreach  (TEXML xCode in xml.ChildElements)
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
