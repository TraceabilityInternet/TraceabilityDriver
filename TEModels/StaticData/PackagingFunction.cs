using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PackageFunction : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageFunction GetFromKey(string key)
        {
            return GetPackageFunctions().Find(c => c.Key == key);
        }

        public static List<PackageFunction> GetPackageFunctions()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PackagingFunctions.xml");
            List<PackageFunction> packages = new List<PackageFunction>();
            foreach (DSXML xPackage in xml.ChildElements)
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
