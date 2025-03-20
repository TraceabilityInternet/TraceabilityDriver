using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class MaturationMethodCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static MaturationMethodCode GetFromKey(string key)
        {
            return GetMaturationMethodCodes().Find(c => c.Key == key);
        }

        public static List<MaturationMethodCode> GetMaturationMethodCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.MaturationMethodCodes.xml");
            List<MaturationMethodCode> codes = new List<MaturationMethodCode>();
            foreach (DSXML xCode in xml.ChildElements)
            {
                MaturationMethodCode code = new MaturationMethodCode();
                code.Key = xCode.Attribute("Key");
                code.Name = xCode.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
