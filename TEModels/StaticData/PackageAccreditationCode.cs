using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PackageAccreditationCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageAccreditationCode GetFromKey(string key)
        {
            return GetPackageAccreditationCodes().Find(c => c.Key == key);
        }

        public static List<PackageAccreditationCode> GetPackageAccreditationCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PackageAccreditationCodes.xml");
            List<PackageAccreditationCode> codes = new List<PackageAccreditationCode>();
            foreach  (DSXML xCode in xml.ChildElements)
            {
                PackageAccreditationCode code = new PackageAccreditationCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
