using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class GrowingMethod : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static GrowingMethod GetFromKey(string key)
        {
            return GetGrowingMethods().Find(c => c.Key == key);
        }

        public static List<GrowingMethod> GetGrowingMethods()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.GrowingMethods.xml");
            List<GrowingMethod> methods = new List<GrowingMethod>();
            foreach  (DSXML xMethod in xml.ChildElements)
            {
                GrowingMethod method = new GrowingMethod();
                method.Key = xMethod.Attribute("Key");
                method.Name = xMethod.Attribute("Name");
                methods.Add(method);
            }
            return methods;
        }
    }
}
