using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class FishingMethod : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static FishingMethod GetFromKey(string key)
        {
            return GetFishingMethods().Find(c => c.Key == key);
        }

        public static List<FishingMethod> GetFishingMethods()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.FishingMethods.xml");
            List<FishingMethod> fishingMethods = new List<FishingMethod>();
            foreach  (DSXML xFishingMethod in xml.ChildElements)
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
