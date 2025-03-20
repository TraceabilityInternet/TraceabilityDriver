using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Util.Interfaces
{
    public interface IDID : IPublicDID
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
        string PrivateKey { get; set; }
        ISimpleSignature Sign(string value, string nunce);
        new string ToString();
        new void Parse(string strValue);
        IPublicDID ToPublicDID();
    }
}
