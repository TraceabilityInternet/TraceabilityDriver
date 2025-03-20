using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public interface ITEEventKDE
    {
        public static ConcurrentDictionary<string, Type> InitializeDictionary = new ConcurrentDictionary<string, Type>();
        public static ITEEventKDE InitializeFromKey(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
                ITEEventKDE kde = null;
                if (InitializeDictionary.TryGetValue(key, out Type type))
                {
                    kde = (ITEEventKDE)Activator.CreateInstance(type);
                }
                return kde;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public string Namespace { get; }
        public string NamespacePrefix { get; }
        public string Name { get; }
        public XElement XmlValue { get; set; }
        public JToken JsonValue { get; set; }
        public Type ValueType { get; }
    }
}
