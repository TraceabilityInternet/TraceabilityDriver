using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class NutritionalClaimCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static NutritionalClaimCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<NutritionalClaimCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.NutritionalClaimCodes.xml");
            List<NutritionalClaimCode> codes = new List<NutritionalClaimCode>();
            foreach  (TEXML xCode in xml.ChildElements)
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
