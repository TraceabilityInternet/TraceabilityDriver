using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PreparationTypeCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PreparationTypeCode GetFromKey(string key)
        {
            return GetPreparationTypeCodes().Find(c => c.Key == key);
        }

        public static List<PreparationTypeCode> GetPreparationTypeCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PreparationTypeCodes.xml");
            List<PreparationTypeCode> codes = new List<PreparationTypeCode>();
            foreach  (DSXML xCode in xml.ChildElements)
            {
                PreparationTypeCode code = new PreparationTypeCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
