using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Configuration;
using TraceabilityEngine.Util;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

namespace TraceabilityEngine.Util
{
    /// <summary>
    /// TELogger is a central class for performing logging.  The class is a static class
    /// and the class is thread safe.
    /// </summary>
    public static class TELogger
    {
        public static void Log(Exception ex, [CallerFilePath] string FileName = "", [CallerLineNumber] int LineNumber = 0, [CallerMemberName] string MethodName = "")
        {

        }

        public static void Log(string Message, [CallerFilePath] string FileName = "", [CallerLineNumber] int LineNumber = 0, [CallerMemberName] string MethodName = "")
        {

        }

        public static void Log(int level, Exception ex, [CallerFilePath] string FileName = "", [CallerLineNumber] int LineNumber = 0, [CallerMemberName] string MethodName = "")
        {
            
        }

        public static void Log(int level, string Message, [CallerFilePath] string FileName = "", [CallerLineNumber] int LineNumber = 0, [CallerMemberName] string MethodName = "")
        {
            
        }
    }
}
