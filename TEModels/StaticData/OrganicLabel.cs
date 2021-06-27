using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class OrganicLabel : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static OrganicLabel GetFromKey(string key)
        {
            return GetOrganicLabels().Find(c => c.Key == key);
        }

        public static List<OrganicLabel> GetOrganicLabels()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.OrganicLabels.xml");
            List<OrganicLabel> codes = new List<OrganicLabel>();
            foreach  (DSXML xCode in xml.ChildElements)
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
