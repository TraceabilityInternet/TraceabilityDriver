using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TraceabilityEngine.Util.Extensions
{
    public static class TEStringExtensions
    {
        private static readonly Regex rePattern = new Regex(@"\{([^\}]+)\}", RegexOptions.Compiled);

        /// <summary>
        /// Shortcut for string.Format. Format string uses named parameters like {name}.
        /// 
        /// Example: 
        /// string s = Format("{age} years old, last name is {name} ", new {age = 18, name = "Foo"});
        ///
        /// </summary>
        /// <param name="format"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string Format<T>(this string pattern, T template)
        {
            Dictionary<string, string> cache = new Dictionary<string, string>();
            return rePattern.Replace(pattern, match =>
            {
                string key = match.Groups[1].Value;
                string value;

                if (!cache.TryGetValue(key, out value))
                {
                    var prop = typeof(T).GetProperty(key);
                    if (prop != null)
                    {
                        value = Convert.ToString(prop.GetValue(template, null));
                    }
                    else
                    {
                        value = "{" + key + "}";
                    }
                    cache.Add(key, value);
                }
                return value;
            });
        }

        /// <summary>
        /// Removes any control characters from the string;
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveControlCharacters(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return (input);
            }
            string output = new string(input.Where(c => !char.IsControl(c)).ToArray());
            return (output);
        }
        public static string Truncate(string str, int maxLength)
        {
            try
            {
                string theFinalValue = str;
                if (!String.IsNullOrEmpty(theFinalValue) && theFinalValue.Length > maxLength)
                {
#if DEBUG
                    // if we are in debug mode, let's log that this happened.
                    TELogger.Log(1, $"Value was truncated on Linkable Object's property. Value=" + str + " | Max Length=" + maxLength + "\nStackTrace=" + Environment.StackTrace);
#endif
                    theFinalValue = theFinalValue.Substring(0, maxLength);
                }
                return theFinalValue;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private static Regex _digitsOnlyRegex = new Regex("^[0-9]+$", RegexOptions.Compiled);
        public static bool IsOnlyDigits(this string str)
        {
            try
            {
                return _digitsOnlyRegex.IsMatch(str);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private static Regex _isURICompatibleCharsRegex = new Regex(@"(.*[^._\-:0-9A-Za-z])", RegexOptions.Compiled);
        public static bool IsURICompatibleChars(this string str)
        {
            try
            {
                return !_isURICompatibleCharsRegex.IsMatch(str);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private static string ValidCharacters = @"[^A-Za-z0-9_\-]";
        public static string MakeURICompatible(this string str)
        {
            string fixedStr = Regex.Replace(str, ValidCharacters, "_");
            return fixedStr;
        }

        public static List<string> ToStringList(this string strs)
        {
            try
            {
                List<string> strList = new List<string>();
                if (!string.IsNullOrEmpty(strs))
                {
                    string[] parts = strs.Split(';');
                    foreach (string str in parts)
                    {
                        strList.Add(str);
                    }
                }
                return (strList);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        private static string WildCardToRegular(this string str)
        {
            return "^" + Regex.Escape(str).Replace("\\?", ".").Replace("\\*", ".*").Replace("\\|", "|") + "$";
        }
        public static bool MatchWildCard(this string str, string expression)
        {
            string expReg = expression.WildCardToRegular();

            if (Regex.IsMatch(str, expReg, RegexOptions.IgnoreCase))
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }
    }
}
