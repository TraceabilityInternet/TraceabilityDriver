using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class AllergenCode : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static AllergenCode GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<AllergenCode> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.Allergens.xml");
            List<AllergenCode> codes = new List<AllergenCode>();
            foreach  (TEXML xAllergen in xml.ChildElements)
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
