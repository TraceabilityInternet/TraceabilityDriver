using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class GS1Category : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static GS1Category GetFromKey(string key)
        {
            return GetGS1Categories().Find(c => c.Key == key);
        }

        public static List<GS1Category> GetGS1Categories()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.GS1Categories.xml");
            List<GS1Category> categories = new List<GS1Category>();
            foreach  (DSXML xCat in xml.ChildElements)
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
