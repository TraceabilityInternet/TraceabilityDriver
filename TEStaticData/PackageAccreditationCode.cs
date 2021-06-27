using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PackageAccreditationCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageAccreditationCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PackageAccreditationCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PackageAccreditationCodes.xml");
            List<PackageAccreditationCode> codes = new List<PackageAccreditationCode>();
            foreach  (TEXML xCode in xml.ChildElements)
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
