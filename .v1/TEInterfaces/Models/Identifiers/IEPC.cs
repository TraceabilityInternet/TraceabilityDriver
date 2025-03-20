using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Interfaces.Models.Identifiers
{
    public enum EPCType
    {
        Class = 0,
        Instance = 1
    };

    public interface IEPC : IBsonSerializer
    {
        public static bool IsNullOrEmpty(IEPC epc)
        {
            if (epc == null || string.IsNullOrWhiteSpace(epc.ToString()))
            {
                return true;
            }
            return false;
        }

        EPCType Type { get; set; }
        IGTIN GTIN { get; set; }
        string SerialLotNumber { get; set; }
        string ToDigitalLinkURL(string baseURL);
    }
}
