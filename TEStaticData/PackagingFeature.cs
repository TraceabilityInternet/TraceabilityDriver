using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PackagingFeature : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackagingFeature GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PackagingFeature> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PackageFeatures.xml");
            List<PackagingFeature> packages = new List<PackagingFeature>();
            foreach (TEXML xPackage in xml.ChildElements)
            {
                PackagingFeature package = new PackagingFeature();
                package.Key = xPackage.Attribute("Key");
                package.Name = xPackage.Attribute("Name");
                packages.Add(package);
            }
            return packages;
        }
    }
}
