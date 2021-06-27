using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Mappers
{
    public class TEMappingException : System.Exception
    {
        public TEMappingException(string msg) : base(msg)
        {

        }

        public TEMappingException(string msg, Exception innerEx) : base(msg, innerEx)
        {

        }
    }
}
