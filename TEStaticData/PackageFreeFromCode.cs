using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PackageFreeFromCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageFreeFromCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PackageFreeFromCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PackageFreeFromCodes.xml");
            List<PackageFreeFromCode> codes = new List<PackageFreeFromCode>();
            foreach  (TEXML xCode in xml.ChildElements)
            {
                PackageFreeFromCode code = new PackageFreeFromCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
