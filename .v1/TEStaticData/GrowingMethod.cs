using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class GrowingMethod : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static GrowingMethod GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<GrowingMethod> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.GrowingMethods.xml");
            List<GrowingMethod> methods = new List<GrowingMethod>();
            foreach  (TEXML xMethod in xml.ChildElements)
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
