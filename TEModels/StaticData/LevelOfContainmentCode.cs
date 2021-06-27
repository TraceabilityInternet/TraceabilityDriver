using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class LevelOfContainmentCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static LevelOfContainmentCode GetFromKey(string key)
        {
            return GetLevelOfContainmentCodes().Find(c => c.Key == key);
        }

        public static List<LevelOfContainmentCode> GetLevelOfContainmentCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.LevelOfContainmentCodes.xml");
            List<LevelOfContainmentCode> codes = new List<LevelOfContainmentCode>();
            foreach  (DSXML xCode in xml.ChildElements)
            {
                LevelOfContainmentCode code = new LevelOfContainmentCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
