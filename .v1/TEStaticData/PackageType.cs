using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PackageType : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageType GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PackageType> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PackageTypes.xml");
            List<PackageType> packTypes = new List<PackageType>();
            foreach  (TEXML xPackageType in xml.ChildElements)
            {
                PackageType packType = new PackageType();
                packType.Key = xPackageType.Attribute("Key");
                packType.Name = xPackageType.Attribute("Name");
                packTypes.Add(packType);
            }
            return packTypes;
        }
    }
}
