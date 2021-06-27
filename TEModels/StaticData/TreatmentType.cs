using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class TreatmentType : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static TreatmentType GetFromKey(string key)
        {
            return GetTreatmentTypes().Find(c => c.Key == key);
        }

        public static List<TreatmentType> GetTreatmentTypes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.TreatmentTypes.xml");
            List<TreatmentType> treatmentTypes = new List<TreatmentType>();
            foreach  (DSXML xTreatmentType in xml.ChildElements)
            {
                TreatmentType treatmentType = new TreatmentType();
                treatmentType.Key = xTreatmentType.Attribute("Key");
                treatmentType.Name = xTreatmentType.Attribute("Name");
                treatmentTypes.Add(treatmentType);
            }
            return treatmentTypes;
        }
    }
}
