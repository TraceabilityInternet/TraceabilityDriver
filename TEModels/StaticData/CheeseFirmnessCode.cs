using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class CheeseFirmnessCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static CheeseFirmnessCode GetFromKey(string key)
        {
            return GetCheeseFirmnessCodes().Find(c => c.Key == key);
        }

        public static List<CheeseFirmnessCode> GetCheeseFirmnessCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.CheeseFirmnessCodes.xml");
            List<CheeseFirmnessCode> codes = new List<CheeseFirmnessCode>();
            foreach (DSXML xCode in xml.ChildElements)
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
