using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TraceabilityEngine.Util
{
    public class TEDataConvert
    {
        public static DateTime? StringToDateTime(string str)
        {
            DateTime? datetime = null;
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    //datetime = DateTime.Parse(str, null, DateTimeStyles.RoundtripKind);
                    datetime = TRParseDateTime(str);
                }
                catch (Exception ex)
                {
                    string Message = ex.Message;
                    //TELogger.Log(0, ex);
                    datetime = null;
                }
            }
            return (datetime);
        }
        static private string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            char lastChar = ' ';
            char c;
            foreach (char ct in str)
            {
                c = ct;
                if (c < 32)
                {
                    c = ' ';
                }
                if (c == ' ' && lastChar == ' ')
                {
                    continue;
                }
                if ((c >= '0' && c <= '9') || c == 'T' || c >= 'P' || c == 'A' || c == 'M' || c == '/' || c == ':' || c == '-' || c == ' ' || c == '.')
                {
                    sb.Append(c);
                    lastChar = c;
                }
                else
                {
                    char cSkipped = c;
                }

            }
            return sb.ToString();
        }
        public static bool IsNumericValue(string stringValue)
        {
            try
            {
                double dbl = System.Convert.ToDouble(stringValue);
                return (true);
            }
            catch
            {

            }
            return (false);
        }

        public static bool IsNumericListValue(string stringValue)
        {
            try
            {
                string[] tokens = stringValue.Split(',');
                if (tokens.Count() > 1)
                {
                    foreach (string str in tokens)
                    {
                        double dbl = System.Convert.ToDouble(str);
                    }
                    return (true);
                }
                return (false);
            }
            catch
            {

            }
            return (false);
        }

        public static bool IsDateValue(string stringValue)
        {
            try
            {
                bool bRet = false;
                if (stringValue.Contains("-") || stringValue.Contains('/'))
                {
                    if (stringValue[0] != '-')
                    {
                        DateTime? dt = TRParseDateTime(stringValue);
                        if (dt != null)
                        {
                            bRet = true;
                        }
                    }
                }
                return (bRet);
            }
            catch
            {

            }
            return (false);
        }

        public static DateTime? ParseJSONDate(string strVal)
        {
            DateTime? dt = null;
            DateTime dtVal;
            bool bOk = false;
            DateTimeStyles style = DateTimeStyles.AdjustToUniversal |
                                   DateTimeStyles.AllowInnerWhite |
                                   DateTimeStyles.AllowLeadingWhite |
                                   DateTimeStyles.AllowTrailingWhite |
                                   DateTimeStyles.AllowWhiteSpaces;
            if (string.IsNullOrEmpty(strVal))
            {
                return (dt);
            }
            strVal = strVal.Replace(" ", "");
            int iLen = strVal.Length;
            string[] formats = new string[6];
            formats[0] = "o";
            formats[1] = "yyyy-MM-ddTHH:mm:ssZ";
            formats[2] = "yyyy-MM-ddTHH:mm:ss zzz";
            formats[3] = "yyyy-MM-ddTHH:mm:ss.f zzz";
            formats[4] = "yyyy-MM-ddTHH:mm:ss.ff zzz";
            formats[5] = "yyyy-MM-ddTHH:mm:ss.fff zzz";

            bOk = DateTime.TryParseExact(strVal, formats, CultureInfo.InvariantCulture, style, out dtVal);
            if (bOk)
            {
                dt = dtVal;
            }
            else
            {
                dt = TEDataConvert.TRParseDateTime(strVal);
            }
            return (dt);

        }

        public static DateTime? TRParseDateTime(string stringValue)
        {
            string cultureName = "en-US";
            CultureInfo culture = new CultureInfo(cultureName);
            DateTime? dt = null;
            DateTime dtOut;
            bool bOk = false;
            try
            {
                if (string.IsNullOrEmpty(stringValue))
                {
                    return (dt);
                }
                stringValue = stringValue.ToUpper();
                stringValue = stringValue.Trim();
                stringValue = RemoveSpecialCharacters(stringValue);
                //Remove duplicate spaces


                if (stringValue.Contains("/"))
                {
                    if (stringValue.Length <= 10)
                    {
                        string[] parts = stringValue.Split('/');
                        int iMonth = System.Convert.ToInt32(parts[0]);
                        int iDay = System.Convert.ToInt32(parts[1]);
                        int iYear = System.Convert.ToInt32(parts[2]);
                        if (iYear < 2000)
                        {
                            iYear += 2000;
                        }
                        dt = new DateTime(iYear, iMonth, iDay);
                    }
                    else
                    {
                        string[] formats =
                        {"M/d/yyyy h:mm:ss tt",
                         "M/d/yyyy h:mm tt",
                         "M/d/yyyy hh:mm tt",
                         "M/d/yyyy hh tt",
                         "MM/dd/yyyy HH:mm:ss",
                         "M/d/yyyy H:mm:ss",
                         "M/d/yyyy HH:mm:ss",
                         "M/d/yyyy H:mm",
                         "M/d/yyyy H:mm",
                         "MM/dd/yyyy HH:mm",
                         "M/dd/yyyy HH:mm" };
                        bOk = DateTime.TryParseExact(stringValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtOut);
                        if (bOk)
                        {
                            dt = dtOut;
                        }
                        else
                        {
                            string[] Parts = stringValue.Split(' ');
                            string DatePart = Parts[0];
                            string TimePart = Parts[1];
                            string[] dateParts = DatePart.Split('/');
                            int iMonth = System.Convert.ToInt32(dateParts[0]);
                            int iDay = System.Convert.ToInt32(dateParts[1]);
                            int iYear = System.Convert.ToInt32(dateParts[2]);

                            string[] TimeParts = TimePart.Split(':');
                            int iHour = System.Convert.ToInt32(TimeParts[0]);
                            int iMin = System.Convert.ToInt32(TimeParts[1]);
                            int iSec = System.Convert.ToInt32(TimeParts[2]);

                            if (stringValue.Contains("PM"))
                            {
                                if (iHour < 12)
                                    iHour += 12;
                            }
                            if (stringValue.Contains("AM"))
                            {
                                if (iHour == 12)
                                {
                                    iHour = 0;
                                }
                            }
                            try
                            {
                                dt = new DateTime(iYear, iMonth, iDay, iHour, iMin, iSec);
                            }
                            catch (Exception)
                            {
                                string strMessage = string.Format("Year:{0} Month:{1} Day:{2} Hour:{3} Min:{4} Sec:{5}", iYear, iMonth, iDay, iHour, iMin, iSec);
                                //TELogger.Log(0, "DateTime failure: " + strMessage);
                                throw;
                            }
                        }
                    }
                    return (dt);
                }
                else if (stringValue.Contains("-"))
                {
                    //2015-11-01T02:25:40.000000000  (shows 9 but should be 7 zeros?)
                    //string[] formats = { "s", "r", "o","yyyy'-'MM'-'dd'T'hh':'mm':'ss'.'f"};
                    string[] formats = { "s", "r", "o", "yyyy-MM-ddThh:mm:ss.f", "yyyy-MM-ddThh:mm:ss", "yyyy-MM-dd hh:mm:ss", "yyyy-MM-dd" };
                    DateTimeStyles dateTimeStyles = DateTimeStyles.None;
                    bOk = DateTime.TryParseExact(stringValue, formats, culture, dateTimeStyles, out dtOut);
                    if (bOk)
                    {
                        dt = dtOut;
                    }
                    else
                    {
                        if (stringValue.Contains("."))
                        {
                            // we need to do a bit more here
                            // first determine if a time adjustment is present
                            if (stringValue.Length > 26)
                            {
                                // leave the milliseconds and set to AssumeLocal
                                dateTimeStyles = DateTimeStyles.AssumeLocal;
                            }
                            else
                            {
                                // else clear milliseconds
                                int iIndex = stringValue.IndexOf('.');
                                stringValue = stringValue.Substring(0, iIndex);
                            }
                        }
                        else if (stringValue.Length >= 19)
                        {
                            // insert 7 zeros as milliseconds
                            stringValue = string.Format("{0}.0000000", stringValue.Substring(0, 19), stringValue.Substring(19));
                            dateTimeStyles = DateTimeStyles.AssumeLocal;
                        }
                        bOk = DateTime.TryParseExact(stringValue, formats, culture, dateTimeStyles, out dtOut);
                        if (bOk)
                        {
                            dt = dtOut;
                        }
                    }
                }
                return (dt);
            }
            catch (Exception)
            {
                //TELogger.Log(0, "Datetime argument: " + stringValue);
                //TELogger.Log(0, ex);
                dt = null;
            }
            return (dt);

        }

        public static Int32? StringToInt32(string str)
        {
            int? iVal = null;
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    iVal = System.Convert.ToInt32(str);
                }
                catch (Exception ex)
                {
                    string Message = ex.Message;
                    //TELogger.Log(0, ex);
                    iVal = null;
                }
            }
            return (iVal);
        }

        public static Int64? StringToInt64(string str)
        {
            Int64? iVal = null;
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    iVal = System.Convert.ToInt64(str);
                }
                catch (Exception ex)
                {
                    string Message = ex.Message;
                    //TELogger.Log(0, ex);
                    iVal = null;
                }
            }
            return (iVal);
        }


        public static double? StringDouble(string str)
        {
            double? dVal = null;
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    dVal = System.Convert.ToDouble(str);
                }
                catch (Exception ex)
                {
                    string Message = ex.Message;
                    //TELogger.Log(0, ex);
                    dVal = null;
                }
            }
            return (dVal);
        }


        public static bool? StringToBool(string str)
        {
            bool? bVal = null;
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    if (str == "0")
                    {
                        bVal = false;
                    }
                    else if (str == "1")
                    {
                        bVal = false;
                    }
                    else
                    {
                        bVal = System.Convert.ToBoolean(str);
                    }
                }
                catch (Exception ex)
                {
                    string Message = ex.Message;
                    //TELogger.Log(0, ex);
                    bVal = null;
                }
            }
            return (bVal);
        }

        /// <summary>
        /// This simple IncrementAmendment will only increment to "Z"
        /// further work would be necessary to increment from Z to AA, etc
        /// </summary>
        /// <param name="str"></param>
        /// <returns>Incremented value as result</returns>
        public static string IncrementAmendment(string str)
        {
            string result = str;
            if (string.IsNullOrEmpty(str) == true)
            {
                result = "A";
            }
            else
            {
                try
                {
                    int val = (char)str[0];
                    result = System.Convert.ToChar(val + 1).ToString();
                }
                catch (Exception ex)
                {
                    string Message = ex.Message;
                    //TELogger.Log(0, ex);
                }
            }
            return result;
        }
    }
}
