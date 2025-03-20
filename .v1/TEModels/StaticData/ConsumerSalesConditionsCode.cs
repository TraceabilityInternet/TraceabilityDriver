using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class ConsumerSalesConditionsCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ConsumerSalesConditionsCode GetFromKey(string key)
        {
            return GetConsumerSalesConditionsCodes().Find(c => c.Key == key);
        }

        public static List<ConsumerSalesConditionsCode> GetConsumerSalesConditionsCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.ConsumerSalesConditionsCodes.xml");
            List<ConsumerSalesConditionsCode> codes = new List<ConsumerSalesConditionsCode>();
            foreach (DSXML xCode in xml.ChildElements)
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
