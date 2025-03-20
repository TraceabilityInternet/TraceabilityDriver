using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class FoodBeverageTargetUse : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static FoodBeverageTargetUse GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<FoodBeverageTargetUse> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.FoodBeverageTargetUses.xml");
            List<FoodBeverageTargetUse> codes = new List<FoodBeverageTargetUse>();
            foreach (TEXML xCode in xml.ChildElements)
            {
                FoodBeverageTargetUse code = new FoodBeverageTargetUse();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
