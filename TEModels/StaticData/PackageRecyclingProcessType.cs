using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PackageRecyclingProcessType : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageRecyclingProcessType GetFromKey(string key)
        {
            return GetPackageRecyclingProcessTypes().Find(c => c.Key == key);
        }

        public static List<PackageRecyclingProcessType> GetPackageRecyclingProcessTypes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PackageRecyclingProcessTypes.xml");
            List<PackageRecyclingProcessType> packages = new List<PackageRecyclingProcessType>();
            foreach (DSXML xPackage in xml.ChildElements)
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
