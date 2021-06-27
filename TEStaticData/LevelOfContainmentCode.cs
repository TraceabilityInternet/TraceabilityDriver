using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class LevelOfContainmentCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static LevelOfContainmentCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<LevelOfContainmentCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.LevelOfContainmentCodes.xml");
            List<LevelOfContainmentCode> codes = new List<LevelOfContainmentCode>();
            foreach  (TEXML xCode in xml.ChildElements)
            {
                LevelOfContainmentCode code = new LevelOfContainmentCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
