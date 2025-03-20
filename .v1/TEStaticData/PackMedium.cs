using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PackMedium : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackMedium GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PackMedium> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PackMediums.xml");
            List<PackMedium> packMediums = new List<PackMedium>();
            foreach  (TEXML xPackMedium in xml.ChildElements)
            {
                PackMedium packMedium = new PackMedium();
                packMedium.Key = xPackMedium.Attribute("Key");
                packMedium.Name = xPackMedium.Attribute("Name");
                packMediums.Add(packMedium);
            }
            return packMediums;
        }
    }
}
