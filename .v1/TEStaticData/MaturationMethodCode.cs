using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class MaturationMethodCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static MaturationMethodCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<MaturationMethodCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.MaturationMethodCodes.xml");
            List<MaturationMethodCode> codes = new List<MaturationMethodCode>();
            foreach (TEXML xCode in xml.ChildElements)
            {
                MaturationMethodCode code = new MaturationMethodCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
