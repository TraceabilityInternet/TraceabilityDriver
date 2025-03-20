using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class SharpnessOfCheeseCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static SharpnessOfCheeseCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<SharpnessOfCheeseCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.SharpnessOfCheeseCodes.xml");
            List<SharpnessOfCheeseCode> codes = new List<SharpnessOfCheeseCode>();
            foreach (TEXML xCode in xml.ChildElements)
            {
                SharpnessOfCheeseCode code = new SharpnessOfCheeseCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
