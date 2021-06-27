using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Util.Extensions
{
    public static class TRCharExtensions
    {
        public static bool IsNumber(this char c)
        {
            try
            {
                bool result = false;
                if (c == '0' || c == '1' || c == '2' || c == '3' || c == '4' || c == '5' || c == '6' || c == '7' || c == '8' || c == '9')
                {
                    result = true;
                }
                return result;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
