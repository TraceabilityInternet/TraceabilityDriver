using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class FishingMethod : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static FishingMethod GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<FishingMethod> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.FishingMethods.xml");
            List<FishingMethod> fishingMethods = new List<FishingMethod>();
            foreach  (TEXML xFishingMethod in xml.ChildElements)
            {
                FishingMethod fishingMethod = new FishingMethod();
                fishingMethod.Key = xFishingMethod.Attribute("Key");
                fishingMethod.Name = xFishingMethod.Attribute("Description");
                fishingMethods.Add(fishingMethod);
            }
            return fishingMethods;
        }
    }
}
