using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PackageShape : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageShape GetFromKey(string key)
        {
            return GetPackageShapes().Find(c => c.Key == key);
        }

        public static List<PackageShape> GetPackageShapes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PackageShapes.xml");
            List<PackageShape> packages = new List<PackageShape>();
            foreach (DSXML xPackage in xml.ChildElements)
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
