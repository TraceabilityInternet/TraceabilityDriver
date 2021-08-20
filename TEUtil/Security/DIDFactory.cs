using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Util.Security
{
    public static class DIDFactory
    {
        public static IDID GenerateNew()
        {
            IDID did = DID.GenerateNew();
            return did;
        }

        public static IDID Parse(string didStr)
        {
            IDID did = null;

            if (!string.IsNullOrEmpty(didStr))
            {
                if (didStr.StartsWith("did:traceabilityengine_v1:"))
                {
                    did = new DID();
                    did.Parse(didStr);
                }
            }

            return did;
        }

        public static IPublicDID ParsePublic(string didStr)
        {
            IPublicDID did = null;

            if (!string.IsNullOrEmpty(didStr))
            {
                if (didStr.StartsWith("did:traceabilityengine_v1:"))
                {
                    did = new PublicDID();
                    did.Parse(didStr);
                }
            }

            return did;
        }
    }
}
