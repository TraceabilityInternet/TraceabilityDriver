using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class NonBinaryLogicCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static NonBinaryLogicCode GetFromKey(string key)
        {
            return GetNonBinaryLogicCodes().Find(c => c.Key == key);
        }

        public static List<NonBinaryLogicCode> GetNonBinaryLogicCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.NonBinaryLogicCodes.xml");
            List<NonBinaryLogicCode> codes = new List<NonBinaryLogicCode>();
            foreach (DSXML xCode in xml.ChildElements)
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
