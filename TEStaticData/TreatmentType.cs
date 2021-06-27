using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class TreatmentType : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static TreatmentType GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<TreatmentType> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.TreatmentTypes.xml");
            List<TreatmentType> treatmentTypes = new List<TreatmentType>();
            foreach  (TEXML xTreatmentType in xml.ChildElements)
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
