using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class FoodBeverageTargetUse : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static FoodBeverageTargetUse GetFromKey(string key)
        {
            return GetFoodBeverageTargetUses().Find(c => c.Key == key);
        }

        public static List<FoodBeverageTargetUse> GetFoodBeverageTargetUses()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.FoodBeverageTargetUses.xml");
            List<FoodBeverageTargetUse> codes = new List<FoodBeverageTargetUse>();
            foreach (DSXML xCode in xml.ChildElements)
            {
                FoodBeverageTargetUse code = new FoodBeverageTargetUse();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
