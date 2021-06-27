using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace TraceabilityEngine.Util
{
    public class EmbeddedResourceLoader
    {
        Dictionary<string, Assembly> m_assemblyMap;

        public EmbeddedResourceLoader()
        {
            m_assemblyMap = new Dictionary<string, Assembly>();
        }

        private Assembly GetAssembly(string assemblyName)
        {
            Assembly assembly = null;
            if (m_assemblyMap.ContainsKey(assemblyName))
            {
                assembly = m_assemblyMap[assemblyName];
            }
            else
            {
                assembly = Assembly.Load(assemblyName);
                m_assemblyMap.Add(assemblyName, assembly);
            }
            return (assembly);
        }

        public byte[] ReadBytes(string ResourceName)
        {
            byte[] raw = null;

            try
            {
                string[] tokens = ResourceName.Split('.');
                Assembly assembly = GetAssembly(tokens[0]);
                using (Stream stream = assembly.GetManifestResourceStream(ResourceName))
                {
                    using (BinaryReader sr = new BinaryReader(stream))
                    {

                        raw = sr.ReadBytes((Int32)stream.Length);
                    }
                }

            }
            catch (Exception ex)
            {
                TELogger.Log(0, "ResourceName = " + ResourceName);
                TELogger.Log(0, ex);
            }
            return raw;

        }

        public string ReadString(string ResourceName)
        {
            string result = string.Empty;
            try
            {
                string[] tokens = ResourceName.Split('.');
                Assembly assembly = GetAssembly(tokens[0]);
                using (Stream stream = assembly.GetManifestResourceStream(ResourceName))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        result = sr.ReadToEnd();
                    }
                }

            }
            catch (Exception ex)
            {
                TELogger.Log(0, "ResourceName = " + ResourceName);
                TELogger.Log(0, ex);
            }
            return result;

        }

        public TEXML ReadXML(string ResourceName)
        {
            string result = string.Empty;
            TEXML xml = new TEXML();
            try
            {
                string[] tokens = ResourceName.Split('.');
                Assembly assembly = GetAssembly(tokens[0]);
                using (Stream stream = assembly.GetManifestResourceStream(ResourceName))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        result = sr.ReadToEnd();
                        xml.LoadFromString(result);
                    }
                }

            }
            catch (Exception ex)
            {
                TELogger.Log(0, "ResourceName = " + ResourceName);
                TELogger.Log(0, ex);
            }
            return xml;

        }

        public Stream ReadStream(string ResourceName)
        {
            Stream stream = null;

            try
            {
                string[] tokens = ResourceName.Split('.');
                Assembly assembly = GetAssembly(tokens[0]);
                stream = assembly.GetManifestResourceStream(ResourceName);

            }
            catch (Exception ex)
            {
                TELogger.Log(0, "ResourceName = " + ResourceName);
                TELogger.Log(0, ex);
            }
            return stream;

        }
    }
}
