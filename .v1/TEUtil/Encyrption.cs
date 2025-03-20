using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util;

namespace TEUtil
{
    public static class Encryption
    {
        public static string Encrypt(string value)
        {
            StreamWriter sw = null;
            try
            {
                UnicodeEncoding uniEncoding = new UnicodeEncoding();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    sw = new StreamWriter(memoryStream, uniEncoding);
                    sw.Write(value);
                    sw.Flush();
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    SHA256CryptoServiceProvider provider = new SHA256CryptoServiceProvider();
                    provider.ComputeHash(memoryStream.ToArray());
                    string strHex = BitConverter.ToString(provider.Hash);
                    strHex = strHex.Replace("-", "");
                    return (strHex);
                }
            }
            catch (Exception ex)
            {
                TELogger.Log(0, ex);
                throw;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Dispose();
                }
            }
        }
    }
}
