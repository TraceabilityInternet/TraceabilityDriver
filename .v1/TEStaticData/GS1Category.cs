using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class GS1Category : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static GS1Category GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<GS1Category> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.GS1Categories.xml");
            List<GS1Category> categories = new List<GS1Category>();
            foreach  (TEXML xCat in xml.ChildElements)
            {
                GS1Category cat = new GS1Category();
                cat.Key = xCat.Attribute("Key");
                cat.Name = xCat.Attribute("Name");
                categories.Add(cat);
            }
            return categories;
        }
    }
}
