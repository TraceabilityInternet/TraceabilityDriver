using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PackageRecyclingSchema : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageRecyclingSchema GetFromKey(string key)
        {
            return GetPackageRecyclingSchemas().Find(c => c.Key == key);
        }

        public static List<PackageRecyclingSchema> GetPackageRecyclingSchemas()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PackageRecyclingSchemas.xml");
            List<PackageRecyclingSchema> packages = new List<PackageRecyclingSchema>();
            foreach (DSXML xPackage in xml.ChildElements)
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
