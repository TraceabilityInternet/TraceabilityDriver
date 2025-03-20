using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class AnatomicalFormCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static AnatomicalFormCode GetFromKey(string key)
        {
            return GetAnatomicalFormCodes().Find(c => c.Key == key);
        }

        public static List<AnatomicalFormCode> GetAnatomicalFormCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.AnatomicalFormCodes.xml");
            List<AnatomicalFormCode> codes = new List<AnatomicalFormCode>();
            foreach (DSXML xCode in xml.ChildElements)
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
