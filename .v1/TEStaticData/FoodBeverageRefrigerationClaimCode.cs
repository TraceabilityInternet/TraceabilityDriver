using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class FoodBeverageRefrigerationClaimCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static FoodBeverageRefrigerationClaimCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<FoodBeverageRefrigerationClaimCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.FoodBeverageRefrigerationClaimCodes.xml");
            List<FoodBeverageRefrigerationClaimCode> codes = new List<FoodBeverageRefrigerationClaimCode>();
            foreach  (TEXML xCode in xml.ChildElements)
            {
                FoodBeverageRefrigerationClaimCode code = new FoodBeverageRefrigerationClaimCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
