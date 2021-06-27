using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace TraceabilityEngine.Util
{

    /// <summary>
    /// The resource must be a file located in the TRUitl project.
    /// The default namespace is TRCommon; so if you place a file in the 
    /// Config/Test.xml  the file nme is Config.Test.xml
    /// </summary>
    //public class EmbeddedResource
    //{
    //    public string ReadString(string FullNameSpace)
    //    {
    //        string result = string.Empty;

    //        using (Stream stream = this.GetType().Assembly.GetManifestResourceStream(FullNameSpace))
    //        {
    //            using (StreamReader sr = new StreamReader(stream))
    //            {
    //                result = sr.ReadToEnd();
    //            }
    //        }
    //        return result;
    //    }
    //    public Stream GetStream(string assemblyName, string FullNameSpace)
    //    {
    //        Assembly resourceAssembly = null;
    //        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
    //        {
    //            string[] parts = a.FullName.Split(',');
    //            string bareName = parts[0];
    //            if (bareName.ToUpper() == assemblyName.ToUpper())
    //            {
    //                resourceAssembly = a;
    //                break;
    //            }
    //        }
    //        if (resourceAssembly == null)
    //        {
    //            throw new Exception("Failed to locate assembly:" + assemblyName);
    //        }
    //        Stream stream = resourceAssembly.GetManifestResourceStream(FullNameSpace);
    //        return stream;
    //    }
    //    public string ReadString(string assemblyName, string FullNameSpace)
    //    {
    //        string result = string.Empty;
    //        Assembly resourceAssembly = null;
    //        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
    //        foreach (Assembly a in assemblies)
    //        {
    //            string[] parts = a.FullName.Split(',');
    //            string bareName = parts[0];
    //            if (bareName.ToUpper() == assemblyName.ToUpper())
    //            {
    //                resourceAssembly = a;
    //                break;
    //            }
    //        }
    //        if (resourceAssembly == null)
    //        {
    //            throw new Exception("Failed to locate assembly:" + assemblyName);
    //        }
    //        using (Stream stream = resourceAssembly.GetManifestResourceStream(FullNameSpace))
    //        {
    //            if (stream == null)
    //            {
    //                throw new Exception("Failed to locate GetManifestResourceStream:" + FullNameSpace);
    //            }
    //            using (StreamReader sr = new StreamReader(stream))
    //            {
    //                result = sr.ReadToEnd();
    //            }
    //        }
    //        return result;
    //    }
    //    public TEXML ReadXML(string FullNameSpace)
    //    {
    //        string result = string.Empty;
    //        result = ReadString(FullNameSpace);
    //        TEXML xml = new TEXML();
    //        xml.LoadFromString(result);
    //        return xml;
    //    }
    //    public TEXML ReadXML(string assemblyName, string FullNameSpace)
    //    {
    //        string result = string.Empty;
    //        result = ReadString(assemblyName, FullNameSpace);
    //        TEXML xml = new TEXML();
    //        xml.LoadFromString(result);
    //        return xml;
    //    }
    //}
}
