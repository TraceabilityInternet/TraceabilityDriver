using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class RearingMethod : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static RearingMethod GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<RearingMethod> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.RearingMethods.xml");
            List<RearingMethod> rearingMethods = new List<RearingMethod>();
            foreach  (TEXML xRearingMethod in xml.ChildElements)
            {
                RearingMethod rearingMethod = new RearingMethod();
                rearingMethod.Key = xRearingMethod.Attribute("Key");
                rearingMethod.Name = xRearingMethod.Attribute("Name");
                rearingMethods.Add(rearingMethod);
            }
            return rearingMethods;
        }
    }
}
