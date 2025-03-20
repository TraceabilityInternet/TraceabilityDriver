using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PackageType : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageType GetFromKey(string key)
        {
            return GetPackageTypes().Find(c => c.Key == key);
        }

        public static List<PackageType> GetPackageTypes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PackageTypes.xml");
            List<PackageType> packTypes = new List<PackageType>();
            foreach  (DSXML xPackageType in xml.ChildElements)
            {
                PackageType packType = new PackageType();
                packType.Key = xPackageType.Attribute("Key");
                packType.Name = xPackageType.Attribute("Name");
                packTypes.Add(packType);
            }
            return packTypes;
        }
    }
}
