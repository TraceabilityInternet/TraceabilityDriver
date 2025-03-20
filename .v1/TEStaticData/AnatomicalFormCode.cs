using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class AnatomicalFormCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static AnatomicalFormCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<AnatomicalFormCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.AnatomicalFormCodes.xml");
            List<AnatomicalFormCode> codes = new List<AnatomicalFormCode>();
            foreach (TEXML xCode in xml.ChildElements)
            {
                AnatomicalFormCode code = new AnatomicalFormCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
