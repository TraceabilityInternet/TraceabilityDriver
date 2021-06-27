using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class DietTypeCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static DietTypeCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<DietTypeCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.DietTypeCodes.xml");
            List<DietTypeCode> codes = new List<DietTypeCode>();
            foreach (TEXML xCode in xml.ChildElements)
            {
                DietTypeCode code = new DietTypeCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
