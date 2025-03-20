using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class SourceAnimal : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static SourceAnimal GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<SourceAnimal> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.SourceAnimals.xml");
            List<SourceAnimal> animals = new List<SourceAnimal>();
            foreach (TEXML xSourceAnimal in xml.ChildElements)
            {
                SourceAnimal animal = new SourceAnimal();
                animal.Key = xSourceAnimal.Attribute("Key");
                animal.Name = xSourceAnimal.Attribute("Name");
                animals.Add(animal);
            }
            return animals;
        }
    }
}
