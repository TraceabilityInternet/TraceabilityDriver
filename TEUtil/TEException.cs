using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Util
{
    public class TEException : System.Exception
    {
        public TEException(string msg) : base(msg)
        {

        }

        public TEException(string msg, Exception ex) : base(msg, ex)
        {

        }
    }
}
