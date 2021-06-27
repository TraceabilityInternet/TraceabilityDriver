using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PackageFreeFromCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PackageFreeFromCode GetFromKey(string key)
        {
            return GetPackageFreeFromCodes().Find(c => c.Key == key);
        }

        public static List<PackageFreeFromCode> GetPackageFreeFromCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PackageFreeFromCodes.xml");
            List<PackageFreeFromCode> codes = new List<PackageFreeFromCode>();
            foreach  (DSXML xCode in xml.ChildElements)
            {
                PackageFreeFromCode code = new PackageFreeFromCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
