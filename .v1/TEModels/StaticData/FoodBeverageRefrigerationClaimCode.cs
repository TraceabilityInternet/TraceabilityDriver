using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class FoodBeverageRefrigerationClaimCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static FoodBeverageRefrigerationClaimCode GetFromKey(string key)
        {
            return GetFoodBeverageRefrigerationClaimCodes().Find(c => c.Key == key);
        }

        public static List<FoodBeverageRefrigerationClaimCode> GetFoodBeverageRefrigerationClaimCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.FoodBeverageRefrigerationClaimCodes.xml");
            List<FoodBeverageRefrigerationClaimCode> codes = new List<FoodBeverageRefrigerationClaimCode>();
            foreach  (DSXML xCode in xml.ChildElements)
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
