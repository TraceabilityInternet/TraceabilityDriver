using Force.Crc32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TraceabilityEngine.Util.Extensions
{
    public static class ObjectExtensionsPM
    {

        public static string GetSHA256Hash(this string str)
        {

            StreamWriter sw = null;
            try
            {
                UnicodeEncoding uniEncoding = new UnicodeEncoding();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    sw = new StreamWriter(memoryStream, uniEncoding);
                    sw.Write(str);
                    sw.Flush();
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    SHA256CryptoServiceProvider provider = new SHA256CryptoServiceProvider();
                    provider.ComputeHash(memoryStream.ToArray());
                    string strHex = BitConverter.ToString(provider.Hash);
                    strHex = strHex.Replace("-", "");
                    return (strHex);
                    //return (Convert.ToBase64String(provider.Hash));
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

        public static string GetSHA256Hash(this byte[] bytes)
        {

            StreamWriter sw = null;
            try
            {
                if (bytes == null)
                {
                    string strVal = "null";
                    return (strVal.GetSHA256Hash());
                }
                UnicodeEncoding uniEncoding = new UnicodeEncoding();
                SHA256CryptoServiceProvider provider = new SHA256CryptoServiceProvider();
                provider.ComputeHash(bytes);
                string strHex = BitConverter.ToString(provider.Hash);
                strHex = strHex.Replace("-", "");
                return (strHex);

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

        public static Int32 GetInt32HashCode(this string strText)
        {
            Int32 hashCode = 0;
            if (!string.IsNullOrEmpty(strText))
            {
                hashCode = (int)Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(strText));
            }
            return (hashCode);
        }

        public static Int16 GetInt16HashCode(this string strText)
        {
            Int64 i64Hash = strText.GetInt64HashCode();
            Int16 hash = (Int16)(i64Hash & 0xFFFF);
            hash ^= (Int16)((i64Hash >> 16) & 0xFFFF);
            hash ^= (Int16)((i64Hash >> 32) & 0xFFFF);
            hash ^= (Int16)((i64Hash >> 48) & 0xFFFF);
            return hash;
        }

        public static Int64 GetInt64HashCode(this string strText)
        {
            Int64 hashCode = 0;
            if (!string.IsNullOrEmpty(strText))
            {
                //Unicode Encode Covering all characterset
                byte[] byteContents = Encoding.Unicode.GetBytes(strText);
                System.Security.Cryptography.SHA256 hash =
                new System.Security.Cryptography.SHA256CryptoServiceProvider();
                byte[] hashText = hash.ComputeHash(byteContents);
                //32Byte hashText separate
                //hashCodeStart = 0~7  8Byte
                //hashCodeMedium = 8~23  8Byte
                //hashCodeEnd = 24~31  8Byte
                //and Fold
                Int64 hashCodeStart = BitConverter.ToInt64(hashText, 0);
                Int64 hashCodeMedium = BitConverter.ToInt64(hashText, 8);
                Int64 hashCodeEnd = BitConverter.ToInt64(hashText, 24);
                hashCode = hashCodeStart ^ hashCodeMedium ^ hashCodeEnd;
            }
            return (hashCode);
        }

        public static string AssemblyQualifiedName(this object instance)
        {
            if (instance == null)
            {
                return (null);
            }
            Type typeofInstance = instance.GetType();
            if (typeofInstance == null)
            {
                throw new TEException("Failed to determine the Type of the object");
            }
            string fullAssemblyName = typeofInstance.AssemblyQualifiedName;
            if (string.IsNullOrEmpty(fullAssemblyName))
            {
                throw new TEException("Failed to obtain AssemblyQualifiedName for type = " + typeofInstance.FullName);
            }
            string[] parts = fullAssemblyName.Split(',');
            if (parts.Length < 2)
            {
                throw new TEException("Expected atleast two parts, Unexpected string return for Assemlby, returned value = " + fullAssemblyName);
            }
            string QualifiedName = parts[0] + "," + parts[1];
            return (QualifiedName);
        }


        public static string TEToString(this DateTime? dt)
        {
            string str = null;
            if (dt.HasValue)
            {
                str = dt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            return (str);
        }

        public static string TEToString(this int integer)
        {
            return integer.ToString();
        }

        public static string TEToString(this int? integer)
        {
            string str = null;
            if (integer.HasValue)
            {
                str = integer.ToString();
            }
            return (str);
        }

        public static string TEToString(this DateTime dt)
        {
            string str = dt.ToString("s");
            return (str);
        }

        public static string TEToString(this double val)
        {
            string str = val.ToString("G10");
            return (str);
        }

        public static string TEToString(this double? val)
        {
            string str = null;
            if (val.HasValue)
            {
                str = val.Value.ToString("G10");
            }
            return (str);
        }

        public static string TEToString(this bool? val)
        {
            string str = null;
            if (val.HasValue)
            {
                str = val.Value.TEToString();
            }
            return (str);
        }

        public static string TEToString(this bool val)
        {
            string str = "false";
            if (val)
            {
                str = "true";
            }
            return (str);
        }
        public static string TEToString(this string val)
        {
            if (val == null)
            {
                return ("");
            }
            else
            {
                val = val.Replace("\r", "");
                return (val.Trim());
            }
        }

        //public static string TEToString(this FAOZone val)
        //{
        //    if (val.Key == null)
        //    {
        //        return ("");
        //    }
        //    else
        //    {
        //        return (val.Key.Trim());
        //    }
        //}

        static public bool IsInteger(this string val)
        {
            bool bRet = false;
            if (val == null)
            {
                return (false);
            }
            else if (string.IsNullOrWhiteSpace(val))
            {
                return (false);
            }
            else
            {
                string val2 = val.Replace("-", "");
                bRet = true;
                foreach (char c in val)
                {
                    if (!char.IsDigit(c))
                    {
                        bRet = false;
                        break;
                    }
                }
            }
            return (bRet);
        }

        static public double Round(this double val)
        {
            double roundedValue = val;
            string strVal = val.ToString("e12", CultureInfo.InvariantCulture);
            roundedValue = System.Convert.ToDouble(strVal);
            return (roundedValue);
        }

        static public double? Round(this double? val)
        {
            if (val == null)
            {
                return (val);
            }
            double? roundedValue = val;
            string strVal = val.Value.ToString("e12", CultureInfo.InvariantCulture);
            roundedValue = System.Convert.ToDouble(strVal);
            return (roundedValue);
        }

    }
}
