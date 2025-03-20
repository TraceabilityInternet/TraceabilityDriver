using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class SourceAnimal : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static SourceAnimal GetFromKey(string key)
        {
            return GetSourceAnimals().Find(c => c.Key == key);
        }

        public static List<SourceAnimal> GetSourceAnimals()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.SourceAnimals.xml");
            List<SourceAnimal> animals = new List<SourceAnimal>();
            foreach (DSXML xSourceAnimal in xml.ChildElements)
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
