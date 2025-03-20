using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.StaticData
{
    public class TELanguage : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static TELanguage GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<TELanguage> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.Languages.xml");
            List<TELanguage> languages = new List<TELanguage>();
            foreach (TEXML xLang in xml.ChildElements)
            {
                TELanguage language = new TELanguage();
                language.Key = xLang.Attribute("Key");
                language.Name = xLang.Attribute("Name");
                languages.Add(language);
            }
            return languages;
        }
    }
}
