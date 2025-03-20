using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class OrganizationRole : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static OrganizationRole GetFromKey(string key)
        {
            return GetOrganizationRoles().Find(c => c.Key == key);
        }

        public static List<OrganizationRole> GetOrganizationRoles()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.OrganizationRoles.xml");
            List<OrganizationRole> codes = new List<OrganizationRole>();
            foreach  (DSXML xCode in xml.ChildElements)
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
