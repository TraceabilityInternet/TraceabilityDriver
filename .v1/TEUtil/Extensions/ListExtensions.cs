using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Util.Extensions
{

    public static class ListExtensions
    {
        public static string ToConcatenatedString(this List<string> inputList)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string str in inputList)
            {
                sb.Append(str);
                sb.Append(";");
            }
            return (sb.ToString());
        }
    }
}
