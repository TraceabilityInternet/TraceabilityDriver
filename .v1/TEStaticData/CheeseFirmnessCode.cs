using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class CheeseFirmnessCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static CheeseFirmnessCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<CheeseFirmnessCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.CheeseFirmnessCodes.xml");
            List<CheeseFirmnessCode> codes = new List<CheeseFirmnessCode>();
            foreach (TEXML xCode in xml.ChildElements)
            {
                CheeseFirmnessCode code = new CheeseFirmnessCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
