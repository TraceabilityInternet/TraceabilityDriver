using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Util
{
    /// <summary>
    /// The class is used to perform certain data and convresion utilities
    /// </summary>
    public static class TEDataUtil
    {
        public static string GetAppDataPath(string AppName = "Dashboard")
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\TraceRegister\" + AppName;
            if (Directory.Exists(appDataPath) == false)
            {
                Directory.CreateDirectory(appDataPath);
            }
            return appDataPath;
        }

        public static int FieldIsEqualTo(DataRow[] rows, string FieldName, string Value)
        {
            int iCount = 0;
            string strValue;
            foreach (DataRow row in rows)
            {
                strValue = FieldToString(row, FieldName);
                if (strValue == Value)
                {
                    ++iCount;
                }
            }
            return (iCount);
        }

        public static int FieldNotEqualTo(DataRow[] rows, string FieldName, string Value)
        {
            int iCount = 0;
            string strValue;
            foreach (DataRow row in rows)
            {
                strValue = FieldToString(row, FieldName);
                if (strValue != Value)
                {
                    ++iCount;
                }
            }
            return (iCount);
        }

        public static bool FieldToBool(DataRow row, string FieldName)
        {
            bool bVal = false;
            try
            {
                bVal = DataRowExtensions.Field<bool>(row, FieldName);
            }
            catch (Exception se)
            {
                TELogger.Log(se);
            }
            return (bVal);
        }

        public static string FieldToString(DataRow row, string FieldName)
        {
            string strFld = null;
            try
            {
                strFld = DataRowExtensions.Field<String>(row, FieldName);
            }
            catch (Exception se)
            {
                TELogger.Log(se);
            }
            return (strFld);
        }

        public static int FieldToInt(DataRow row, string FieldName)
        {
            int intFld = 0;
            try
            {
                intFld = DataRowExtensions.Field<int>(row, FieldName);
            }
            catch (Exception se)
            {
                TELogger.Log(se);
            }
            return (intFld);
        }

        public static Int64 FieldToInt64(DataRow row, string FieldName)
        {
            Int64 intFld = 0;
            try
            {
                intFld = DataRowExtensions.Field<Int64>(row, FieldName);
            }
            catch (Exception se)
            {
                TELogger.Log(se);
            }
            return (intFld);
        }

        public static double FieldToDouble(DataRow row, string FieldName)
        {
            double val = double.NaN;
            try
            {
                if (row[FieldName] is DBNull)
                {
                    val = double.NaN;
                }
                else
                {
                    val = DataRowExtensions.Field<double>(row, FieldName);
                }
            }
            catch (Exception se)
            {
                TELogger.Log(se);
            }
            return (val);
        }

        public static DateTime FieldToDateTime(DataRow row, string FieldName)
        {
            DateTime dtFld = new DateTime();
            try
            {
                dtFld = DataRowExtensions.Field<DateTime>(row, FieldName);
            }
            catch (Exception se)
            {
                TELogger.Log(se);
            }
            return (dtFld);
        }

        public static string ReplaceEscapeChars(string str)
        {
            //If the string is null
            if (str == null)
                return str;

            //If the string is empty
            if (str == "")
                return str;

            //Replaces single quote (') with two (2) single quotes ('')
            str = str.Replace("'", "''");
            return str;
        }
    }
}
