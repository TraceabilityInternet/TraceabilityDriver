using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class ColourCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ColourCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<ColourCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.ColourCodes.xml");
            List<ColourCode> codes = new List<ColourCode>();
            foreach  (TEXML xCode in xml.ChildElements)
            {
                ColourCode code = new ColourCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
