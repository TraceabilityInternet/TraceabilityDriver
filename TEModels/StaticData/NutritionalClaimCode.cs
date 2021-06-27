using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class NutritionalClaimCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static NutritionalClaimCode GetFromKey(string key)
        {
            return GetNutritionalClaimCodes().Find(c => c.Key == key);
        }

        public static List<NutritionalClaimCode> GetNutritionalClaimCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.NutritionalClaimCodes.xml");
            List<NutritionalClaimCode> codes = new List<NutritionalClaimCode>();
            foreach  (DSXML xCode in xml.ChildElements)
            {
                NutritionalClaimCode code = new NutritionalClaimCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
