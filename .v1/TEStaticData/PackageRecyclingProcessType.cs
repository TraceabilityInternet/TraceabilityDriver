using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PackageRecyclingProcessType : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageRecyclingProcessType GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PackageRecyclingProcessType> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PackageRecyclingProcessTypes.xml");
            List<PackageRecyclingProcessType> packages = new List<PackageRecyclingProcessType>();
            foreach (TEXML xPackage in xml.ChildElements)
            {
                PackageRecyclingProcessType package = new PackageRecyclingProcessType();
                package.Key = xPackage.Attribute("Key");
                package.Name = xPackage.Attribute("Name");
                packages.Add(package);
            }
            return packages;
        }
    }
}
