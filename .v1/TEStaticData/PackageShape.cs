using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PackageShape : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageShape GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PackageShape> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PackageShapes.xml");
            List<PackageShape> packages = new List<PackageShape>();
            foreach (TEXML xPackage in xml.ChildElements)
            {
                PackageShape package = new PackageShape();
                package.Key = xPackage.Attribute("Key");
                package.Name = xPackage.Attribute("Name");
                packages.Add(package);
            }
            return packages;
        }
    }
}
