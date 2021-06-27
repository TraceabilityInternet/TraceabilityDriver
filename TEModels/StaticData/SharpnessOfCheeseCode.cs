using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class SharpnessOfCheeseCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static SharpnessOfCheeseCode GetFromKey(string key)
        {
            return GetSharpnessOfCheeseCodes().Find(c => c.Key == key);
        }

        public static List<SharpnessOfCheeseCode> GetSharpnessOfCheeseCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.SharpnessOfCheeseCodes.xml");
            List<SharpnessOfCheeseCode> codes = new List<SharpnessOfCheeseCode>();
            foreach (DSXML xCode in xml.ChildElements)
            {
                SharpnessOfCheeseCode code = new SharpnessOfCheeseCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
