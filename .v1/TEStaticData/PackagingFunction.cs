using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PackageFunction : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageFunction GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PackageFunction> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PackagingFunctions.xml");
            List<PackageFunction> packages = new List<PackageFunction>();
            foreach (TEXML xPackage in xml.ChildElements)
            {
                PackageFunction package = new PackageFunction();
                package.Key = xPackage.Attribute("Key");
                package.Name = xPackage.Attribute("Name");
                packages.Add(package);
            }
            return packages;
        }
    }
}
