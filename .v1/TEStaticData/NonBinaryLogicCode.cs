using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class NonBinaryLogicCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static NonBinaryLogicCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<NonBinaryLogicCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.NonBinaryLogicCodes.xml");
            List<NonBinaryLogicCode> codes = new List<NonBinaryLogicCode>();
            foreach (TEXML xCode in xml.ChildElements)
            {
                NonBinaryLogicCode code = new NonBinaryLogicCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
