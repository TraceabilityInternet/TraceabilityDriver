using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityEngine.Util.Security
{
    public static class SimpleSignatureFactory
    {
        public static ISimpleSignature Parse(string value)
        {
            ISimpleSignature signature = null;

            if (!string.IsNullOrWhiteSpace(value))
            {
                signature = new SimpleSignature();
                signature.Parse(value);
            }

            return signature;
        }
    }
}
