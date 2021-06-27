using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class PreservationTechnique : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PreservationTechnique GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<PreservationTechnique> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.PreservationTechniques.xml");
            List<PreservationTechnique> techniques = new List<PreservationTechnique>();
            foreach (TEXML xPreservationTechnique in xml.ChildElements)
            {
                PreservationTechnique technique = new PreservationTechnique();
                technique.Key = xPreservationTechnique.Attribute("Key");
                technique.Name = xPreservationTechnique.Attribute("Name");
                techniques.Add(technique);
            }
            return techniques;
        }
    }
}
