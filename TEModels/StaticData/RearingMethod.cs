using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class RearingMethod : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static RearingMethod GetFromKey(string key)
        {
            return GetRearingMethods().Find(c => c.Key == key);
        }

        public static List<RearingMethod> GetRearingMethods()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.RearingMethods.xml");
            List<RearingMethod> rearingMethods = new List<RearingMethod>();
            foreach  (DSXML xRearingMethod in xml.ChildElements)
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
