using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace TraceabilityEngine.Util.Extensions
{
    public static class TEXMLExtentions
    {
        public static TEXML LoadFromResource(string assemblyName, string resourceName)
        {
            TEXML xmlNew = null;
            try
            {
                EmbeddedResourceLoader embeddedResource = new EmbeddedResourceLoader();
                xmlNew = embeddedResource.ReadXML(resourceName);
                return (xmlNew);
            }
            catch (Exception ex)
            {
                TELogger.Log(0, ex);
                throw;
            }
        }
        public static void ResolveFileReferences(this TEXML xml, string BasePath, bool bRecurse = true)
        {
            string FileName = xml.Attribute("File");
            if (!string.IsNullOrEmpty(FileName) && (xml.CData == null || string.IsNullOrEmpty(xml.CData.Data)))
            {
                if (!string.IsNullOrEmpty(BasePath))
                {
                    if (BasePath.EndsWith("\\"))
                    {
                        FileName = BasePath + FileName;
                    }
                    else
                    {
                        FileName = BasePath + "\\" + FileName;
                    }
                }
                if (System.IO.File.Exists(FileName))
                {
                    TextReader tr = File.OpenText(FileName);
                    string strValue = tr.ReadToEnd();
                    XmlCDataSection cdata = xml.Doc.CreateCDataSection(strValue);
                    xml.Element.AppendChild(cdata);
                }
                else
                {
                    TELogger.Log(0, "File not found: " + FileName);
                    throw new Exception("File not found: " + FileName);
                }
            }
            else if (xml.HasAttribute("Resource") && (xml.CData == null || string.IsNullOrEmpty(xml.CData.Data)))
            {
                string resourceName = "";
                string assemblyName = "";
                try
                {
                    EmbeddedResourceLoader embeddedResource = new EmbeddedResourceLoader();
                    resourceName = xml.Attribute("Resource");
                    assemblyName = xml.Attribute("Assembly");
                    string strValue = embeddedResource.ReadString(resourceName);
                    XmlCDataSection cdata = xml.Doc.CreateCDataSection(strValue);
                    xml.Element.AppendChild(cdata);
                }
                catch (Exception ex)
                {
                    TELogger.Log(0, string.Format("ResourceName={0} and assemblyName={1}", resourceName, assemblyName));
                    TELogger.Log(0, ex);
                    throw;
                }

            }
            if (bRecurse)
            {
                for (TEXML xmlChild = xml.FirstChild; !xmlChild.IsNull; xmlChild = xmlChild.NextSibling)
                {
                    xmlChild.ResolveFileReferences(BasePath, bRecurse);
                }
            }
        }
        public static void ExpandSchemaForIncludes(this TEXML xml)
        {
            bool bRet = xml.ExpandSchemaForIncludes_Internal();
            while (bRet == true)
            {
                bRet = xml.ExpandSchemaForIncludes_Internal();
            }

        }
        private static bool ExpandSchemaForIncludes_Internal(this TEXML xmlarg)
        {
            List<TEXML> includes = xmlarg.Elements("//IncludeSchema");
            foreach (TEXML xml in includes)
            {
                TEXML xmlNew = new TEXML();
                if (xml.HasAttribute("File"))
                {
                    string FileName = xml.Attribute("File");
                    if (!File.Exists(FileName))
                    {
                        FileName = AppDomain.CurrentDomain.BaseDirectory + FileName;
                        if (!File.Exists(FileName))
                        {
                            TELogger.Log(0, "Include Filename not found:" + FileName);
                            return (false);
                        }
                    }
                    xmlNew.Load(FileName);
                }
                else if (xml.HasAttribute("Resource"))
                {
                    string resourceName = "";
                    string assemblyName = "";
                    try
                    {
                        EmbeddedResourceLoader embeddedResource = new EmbeddedResourceLoader();
                        resourceName = xml.Attribute("Resource");
                        assemblyName = xml.Attribute("Assembly");
                        xmlNew = embeddedResource.ReadXML(resourceName);
                    }
                    catch (Exception ex)
                    {
                        TELogger.Log(0, string.Format("ResourceName={0} and assemblyName={1}", resourceName, assemblyName));
                        TELogger.Log(0, ex);
                        throw;
                    }
                }

                TEXML xmlRefSibling = xml;
                for (TEXML xmlChild = xmlNew.FirstChild; !xmlChild.IsNull; xmlChild = xmlChild.NextSibling)
                {
                    xmlRefSibling = xmlarg.InsertChildAfter(xmlChild, xmlRefSibling);
                }
                xmlarg.RemoveChild(xml);
            }
            string str = xmlarg.XmlString;
            str = str.Replace("&#xD;&#xA;", " ");
            xmlarg.LoadFromString(str);
            return (includes.Count > 0);
        }
        public static void ExpandBaseSchemaReferences(this TEXML xmlarg)
        {
            List<TEXML> includes = xmlarg.Elements("//Table[@BaseSchema]");
            foreach (TEXML xmltable in includes)
            {
                string resourcePath = xmltable.Attribute("BaseSchema");
                string[] parts = resourcePath.Split(',');
                if (parts.Length != 2)
                {
                    throw new TEException("Invalid baseschema resource reference," + resourcePath);
                }
                TEXML xmlBaseSchema = TEXMLExtentions.LoadFromResource(parts[0], parts[1]);
                foreach (TEXML xmlField in xmlBaseSchema["TableSchema"])
                {
                    xmltable.AddChild(xmlField);
                }
            }
        }
    }
}
