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
        private static ConcurrentDictionary<string, Type> _initializeDictionary = new ConcurrentDictionary<string, Type>();
        static ITEEventKDE()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Where(t => typeof(ITEEventKDE).IsAssignableFrom(t));
            foreach (var t in types)
            {
                string typeKey = TEEnumUtil.GetEnumKey(t);
                _initializeDictionary.TryAdd(typeKey, t);
            }
        }
        public static ITEEventKDE InitializeFromKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            ITEEventKDE kde = null;
            if (_initializeDictionary.TryGetValue(key, out Type type))
            {
                kde = (ITEEventKDE)Activator.CreateInstance(type);
            }
            return kde;
        }

        public string Namespace { get; }
        public string NamespacePrefix { get; }
        public string Name { get; }
        public XElement XmlValue { get; set; }
        public JToken JsonValue { get; set; }
        public Type ValueType { get; }
    }
}
