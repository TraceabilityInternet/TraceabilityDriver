using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PreparationTypeCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PreparationTypeCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PreparationTypeCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PreparationTypeCodes.xml");
            List<PreparationTypeCode> codes = new List<PreparationTypeCode>();
            foreach  (TEXML xCode in xml.ChildElements)
            {
                PreparationTypeCode code = new PreparationTypeCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
