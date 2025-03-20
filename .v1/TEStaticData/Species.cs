using TraceabilityEngine.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace TraceabilityEngine.StaticData
{
    public static class SpeciesList
    {
        private static List<Species> m_Entries = null;
        private static object m_Locker = new object();
        private static ConcurrentDictionary<string, Species> m_SpeciesByScientficName = null;
        private static ConcurrentDictionary<string, Species> m_SpeciesByAlphaCode = null;
        private static ConcurrentDictionary<string, Species> m_SpeciesByEnglishName = null;

        public static List<Species> Entries
        {
            get
            {
                if (m_Entries == null)
                {
                    Initialize();
                }
                return m_Entries;
            }
        }

        static public DataTable GetDataTable()
        {
            DataTable dt = new DataTable("Species");
            dt.Columns.Add("ScientificName");
            dt.Columns.Add("Synonyms");
            dt.Columns.Add("Alpha3Code");
            dt.Columns.Add("Description");
            dt.Columns.Add("MarketNames");
            dt.Columns.Add("EnglishName");

            foreach (Species item in Entries)
            {
                DataRow row = dt.NewRow();
                row["ScientificName"] = item.ScientificName;
                row["Synonyms"] = item.Synonyms;
                row["Alpha3Code"] = item.Alpha3Code;
                row["Description"] = item.Description;
                row["MarketNames"] = item.MarketNames;
                row["EnglishName"] = item.EnglishName;
                dt.Rows.Add(row);
            }
            return (dt);
        }

        private static void Initialize()
        {
            if (m_Entries == null)
            {
                lock (m_Locker)
                {
                    //check to make sure this thread is the first one to obtain the lock;
                    if (m_Entries == null)
                    {
                        m_Entries = Load();
                        m_SpeciesByScientficName = new ConcurrentDictionary<string, Species>();
                        m_SpeciesByAlphaCode = new ConcurrentDictionary<string, Species>();
                        m_SpeciesByEnglishName = new ConcurrentDictionary<string, Species>();
                        foreach (Species species in m_Entries)
                        {
                            m_SpeciesByScientficName.TryAdd(species.ScientificName.ToUpper(), species);
                            m_SpeciesByAlphaCode.TryAdd(species.Alpha3Code.ToUpper(), species);
                            if (species.EnglishName != null)
                            {
                                m_SpeciesByEnglishName.TryAdd(species.EnglishName.ToUpper(), species);
                            }
                        }
                    }
                }
            }
        }


        public static Species GetSpeciesByScientificName(string scientificName)
        {
            Species species = null;
            Initialize();
            if (!string.IsNullOrEmpty(scientificName))
            {
                scientificName = scientificName.ToUpper().Trim();
                if (m_SpeciesByScientficName.ContainsKey(scientificName))
                {
                    species = m_SpeciesByScientficName[scientificName];
                }
                else
                {
                    foreach (KeyValuePair<string, Species> kvp in m_SpeciesByScientficName)
                    {
                        if (!string.IsNullOrEmpty(kvp.Value.Synonyms))
                        {
                            if (kvp.Value.Synonyms.ToUpper() == scientificName)
                            {
                                species = kvp.Value;
                                break;
                            }
                        }
                    }
                }
            }
            return (species);
        }
        public static Species GetSpeciesByAlphaCode(string alphaCode)
        {
            Species species = null;
            Initialize();
            if (!string.IsNullOrEmpty(alphaCode))
            {
                alphaCode = alphaCode.ToUpper().Trim();
                if (m_SpeciesByAlphaCode.ContainsKey(alphaCode))
                {
                    species = m_SpeciesByAlphaCode[alphaCode];
                }
            }
            return (species);
        }

        public static Species GetSpeciesByEnglishName(string englishName)
        {
            Species species = null;
            Initialize();
            if (!string.IsNullOrEmpty(englishName))
            {
                englishName = englishName.Trim();
                englishName = englishName.Replace("-", "");
                englishName = englishName.ToUpper();
                if (m_SpeciesByEnglishName.ContainsKey(englishName))
                {
                    species = m_SpeciesByEnglishName[englishName];
                }
            }
            return (species);
        }

        private static List<Species> Load()
        {
            List<Species> methods = new List<Species>();
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xmlSpeciesList = loader.ReadXML("TEStaticData.Data.SpeciesList.xml");
            foreach (TEXML xmlSpecies in xmlSpeciesList)
            {
                Species metod = new Species(xmlSpecies);
                methods.Add(metod);
            }
            methods = methods.OrderBy(x => x.Key).ToList();
            return (methods);
        }
    }

    [DataContract]
    public class Species : IEquatable<Species>, IComparable<Species>
    {
        public Species()
        {
        }

        public Species(TEXML xml)
        {
            Key = xml.Attribute("Key");
            Description = xml.Attribute("Description");
            ScientificName = xml.Attribute("ScientificName");
            Synonyms = xml.Attribute("Synonyms");
            Alpha3Code = xml.Attribute("Alpha3Code");
            Family = xml.Attribute("Family");
            SIMPRequired = xml.AttributeBoolValue("SIMPRequired");
            MarketNames = xml.Attribute("MarketNames");
            EnglishName = xml.Attribute("EnglishName");


        }
        public string Key { get; set; }
        public string Description { get; set; }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                if (obj is Species)
                {
                    Species other = (Species)obj;
                    if (Key == other.Key)
                    {
                        return (true);
                    }
                }
            }
            return (false);
        }

        public override int GetHashCode()
        {
            if (Key != null)
            {
                return (Key.GetHashCode());
            }
            else
            {
                return (base.GetHashCode());
            }

        }

        public bool IsDataComplete()
        {
            try
            {
                if (string.IsNullOrEmpty(ScientificName))
                {
                    return (false);
                }
                return (true);
            }
            catch (Exception ex)
            {
                TELogger.Log(0, ex);
                throw;
            }
        }

        public static List<Species> GetList()
        {
            return SpeciesList.Entries;
        }

        [DataMember]
        [XmlElement]
        public string ScientificName { get; set; }

        [DataMember]
        [XmlElement]
        public string Synonyms { get; set; }

        [DataMember]
        [XmlElement]
        public string Alpha3Code { get; set; }

        [DataMember]
        [XmlElement]
        public string Family { get; set; }

        [DataMember]
        [XmlElement]
        public bool SIMPRequired { get; set; }

        [DataMember]
        [XmlElement]
        public string MarketNames { get; set; }

        [DataMember]
        [XmlElement]
        public string EnglishName { get; set; }

        public TEXML ToXML(TEXML xmlParent, string name = "Species")
        {
            TEXML xmlFM = xmlParent.AddChild(name);
            xmlFM.Attribute("Key", Key);
            return (xmlFM);
        }

        public static Species FromXML(TEXML xmlFM)
        {
            string key = xmlFM.Attribute("Key");
            Species species = SpeciesList.GetSpeciesByScientificName(key);
            return (species);
        }


        public override string ToString()
        {
            return this.Alpha3Code;
        }



        public bool Equals(Species other)
        {
            return (Key == other.Key);
        }

        public int CompareTo(Species other)
        {
            return (Key.CompareTo(other.Key));
        }
    }
}
