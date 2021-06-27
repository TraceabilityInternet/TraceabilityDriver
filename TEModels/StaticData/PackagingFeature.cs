using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PackagingFeature : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackagingFeature GetFromKey(string key)
        {
            return GetPackagingFeatures().Find(c => c.Key == key);
        }

        public static List<PackagingFeature> GetPackagingFeatures()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PackageFeatures.xml");
            List<PackagingFeature> packages = new List<PackagingFeature>();
            foreach (DSXML xPackage in xml.ChildElements)
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
