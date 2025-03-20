using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class ConsumerSalesConditionsCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ConsumerSalesConditionsCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<ConsumerSalesConditionsCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.ConsumerSalesConditionsCodes.xml");
            List<ConsumerSalesConditionsCode> codes = new List<ConsumerSalesConditionsCode>();
            foreach (TEXML xCode in xml.ChildElements)
            {
                ConsumerSalesConditionsCode code = new ConsumerSalesConditionsCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
