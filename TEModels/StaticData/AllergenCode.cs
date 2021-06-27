using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class AllergenCode : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static AllergenCode GetFromKey(string key)
        {
            return GetAllergenCodes().Find(c => c.Key == key);
        }

        public static List<AllergenCode> GetAllergenCodes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.Allergens.xml");
            List<AllergenCode> codes = new List<AllergenCode>();
            foreach  (DSXML xAllergen in xml.ChildElements)
            {
                AllergenCode code = new AllergenCode();
                code.Key = xAllergen.Attribute("Key");
                code.Name = xAllergen.Attribute("Name");
                codes.Add(code);
            }
            return codes;
        }
    }
}
