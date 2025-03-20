using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Util.Interfaces
{
    public interface ISimpleSignature
    {
        public string Value { get; set; }

        public string Nunce { get; set; }

        public string Signature { get; set; }

        string ToString();
        void Parse(string strValue);
    }
}
