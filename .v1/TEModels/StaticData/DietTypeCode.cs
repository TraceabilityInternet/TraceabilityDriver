using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class DietTypeCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static DietTypeCode GetFromKey(string key)
        {
            return GetDietTypeCodes().Find(c => c.Key == key);
        }

        public static List<DietTypeCode> GetDietTypeCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.DietTypeCodes.xml");
            List<DietTypeCode> codes = new List<DietTypeCode>();
            foreach (DSXML xCode in xml.ChildElements)
            {
                DietTypeCode code = new DietTypeCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
