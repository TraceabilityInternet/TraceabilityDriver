using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Util.Interfaces
{
    /// <summary>
    /// This is a DID with just the public key.
    /// </summary>
    public interface IPublicDID
    {
        public static bool IsNullOrEmpty(IPublicDID did)
        {
            if (did == null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(did.ID))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(did.PublicKey))
            {
                return true;
            }

            return false;
        }

        string ID { get; }
        string PublicKey { get; set; }
        bool Verify(string value, string nunce, string signature);
        bool Verify(ISimpleSignature signature);
        string ToString();
        void Parse(string strValue);
    }
}
