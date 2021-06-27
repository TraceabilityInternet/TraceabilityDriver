using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class ProductTypeCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductTypeCode GetFromKey(string key)
        {
            return GetProductTypeCodes().Find(c => c.Key == key);
        }

        public static List<ProductTypeCode> GetProductTypeCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.ProductTypeCodes.xml");
            List<ProductTypeCode> codes = new List<ProductTypeCode>();
            foreach (DSXML xCode in xml.ChildElements)
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
