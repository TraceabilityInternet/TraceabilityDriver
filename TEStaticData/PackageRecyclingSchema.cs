using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PackageRecyclingSchema : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageRecyclingSchema GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PackageRecyclingSchema> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PackageRecyclingSchemas.xml");
            List<PackageRecyclingSchema> packages = new List<PackageRecyclingSchema>();
            foreach (TEXML xPackage in xml.ChildElements)
            {
                PackageRecyclingSchema package = new PackageRecyclingSchema();
                package.Key = xPackage.Attribute("Key");
                package.Name = xPackage.Attribute("Name");
                packages.Add(package);
            }
            return packages;
        }
    }
}
