using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class ColourCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ColourCode GetFromKey(string key)
        {
            return GetColourCodes().Find(c => c.Key == key);
        }

        public static List<ColourCode> GetColourCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.ColourCodes.xml");
            List<ColourCode> codes = new List<ColourCode>();
            foreach  (DSXML xCode in xml.ChildElements)
            {
                ColourCode code = new ColourCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
