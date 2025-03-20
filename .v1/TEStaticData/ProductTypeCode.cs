using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class ProductTypeCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductTypeCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<ProductTypeCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.ProductTypeCodes.xml");
            List<ProductTypeCode> codes = new List<ProductTypeCode>();
            foreach (TEXML xCode in xml.ChildElements)
            {
                ProductTypeCode code = new ProductTypeCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
