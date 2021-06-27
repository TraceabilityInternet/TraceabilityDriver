using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class PreservationTechnique : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static PreservationTechnique GetFromKey(string key)
        {
            return GetPreservationTechniques().Find(c => c.Key == key);
        }

        public static List<PreservationTechnique> GetPreservationTechniques()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.PreservationTechniques.xml");
            List<PreservationTechnique> techniques = new List<PreservationTechnique>();
            foreach (DSXML xPreservationTechnique in xml.ChildElements)
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
