using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Interfaces.Services.DirectoryService
{
    public interface ITEDirectoryAccount : ITEDocumentObject
    {
        long ID { get; set; }
        string Name { get; set; }
        IPGLN ServiceProviderPGLN { get; set; }
        IPGLN PGLN { get; set; }
        IDID DID { get; set; }
        string DigitalLinkURL { get; set; }
        string ToJson();
        void FromJson(string json);
        ITEDriverTradingPartner ToDriverTradingPartner();

        public static bool IsNullOrEmpty(ITEDirectoryAccount account)
        {
            if (account == null)
            {
                return true;
            }

            if (IPGLN.IsNullOrEmpty(account.PGLN))
            {
                return true;
            }

            if (IDID.IsNullOrEmpty(account.DID))
            {
                return true;
            }

            return false;
        }
    }
}
