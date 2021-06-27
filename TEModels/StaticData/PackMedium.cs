using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PackMedium : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackMedium GetFromKey(string key)
        {
            return GetPackMediums().Find(c => c.Key == key);
        }

        public static List<PackMedium> GetPackMediums()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PackMediums.xml");
            List<PackMedium> packMediums = new List<PackMedium>();
            foreach  (DSXML xPackMedium in xml.ChildElements)
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
