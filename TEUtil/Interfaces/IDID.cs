using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Util.Interfaces
{
    public interface IDID
    {
        public static bool IsNullOrEmpty(IDID did)
        {
            if (did == null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(did.ID))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(did.PublicKey) && string.IsNullOrWhiteSpace(did.PrivateKey))
            {
                return true;
            }

            return false;
        } 

        string ID { get; }
        string PublicKey { get; set; }
        string PrivateKey { get; set; }
        ISimpleSignature Sign(string value, string nunce);
        bool Verify(string value, string nunce, string signature);
        bool Verify(ISimpleSignature signature);
        string ToString();
        void Parse(string strValue);
    }
}
