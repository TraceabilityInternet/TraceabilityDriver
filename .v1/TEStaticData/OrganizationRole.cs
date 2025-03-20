using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class OrganizationRole : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static OrganizationRole GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<OrganizationRole> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.OrganizationRoles.xml");
            List<OrganizationRole> codes = new List<OrganizationRole>();
            foreach  (TEXML xCode in xml.ChildElements)
            {
                OrganizationRole code = new OrganizationRole();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
