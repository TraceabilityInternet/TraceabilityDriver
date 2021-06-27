using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TraceabilityEngine.Util.StaticData
{
    public class TEStaticData
    {
        public static string ReadData(string path)
        {
            string result = string.Empty;
            using (Stream stream = typeof(TEStaticData).Assembly.
                       GetManifestResourceStream("TEUtil.StaticData.Data." + path))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    result = sr.ReadToEnd();
                }
            }
            return result;
        }
    }
}
