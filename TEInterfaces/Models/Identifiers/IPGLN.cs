using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Interfaces.Models.Identifiers
{
    public interface IPGLN : IEquatable<IPGLN> 
    {
        public static bool IsNullOrEmpty(IPGLN pgln)
        {
            if (pgln == null)
            {
                return true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(pgln.ToString()))
                {
                    return true;
                }
            }
            return false;
        }

        string ToDigitalLinkURL(string baseURL);
    }
}
