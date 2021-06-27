using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class OrganicLabel : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static OrganicLabel GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<OrganicLabel> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.OrganicLabels.xml");
            List<OrganicLabel> codes = new List<OrganicLabel>();
            foreach  (TEXML xCode in xml.ChildElements)
            {
                OrganicLabel code = new OrganicLabel();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
