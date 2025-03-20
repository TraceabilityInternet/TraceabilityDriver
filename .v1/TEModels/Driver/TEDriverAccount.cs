using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityEngine.Models.Driver
{
    public class TEDriverAccount : ITEDriverAccount
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ObjectID { get; set; }


        public string DigitalLinkURL { get; set; }
        public long ID { get; set; }
        public string Name { get; set; }
        public IPGLN PGLN { get; set; }
        public IDID DID { get; set; }

        public ITEDirectoryNewAccount ToDirectoryAccount(IDID serviceProviderDID, IPGLN serviceProviderPGLN)
        {
            ITEDirectoryNewAccount newAccount = new TEDirectoryNewAccount();

            newAccount.DID = this.DID;
            newAccount.PublicDID = this.DID.ToPublicDID();
            newAccount.DigitalLinkURL = this.DigitalLinkURL;
            newAccount.PGLN = this.PGLN;
            newAccount.ServiceProviderDID = serviceProviderDID;
            newAccount.ServiceProviderPGLN = serviceProviderPGLN;
            newAccount.Sign();
            newAccount.ID = this.ID; // John edit, b/c directoryAccount.ID was not matching the accounts being registered to the directory.

            return newAccount;
        }

        public string ToJson()
        {
            JObject json = new JObject();
            json["DigitalLinkURL"] = this.DigitalLinkURL;
            json["DID"] = this.DID?.ToString();
            json["PublicDID"] = this.DID?.ToPublicDID().ToString();
            json["Name"] = this.Name;
            json["ID"] = this.ID;
            json["PGLN"] = this.PGLN?.ToString();
            return json.ToString();
        }

        public void FromJson(string jsonStr)
        {
            JObject json = JObject.Parse(jsonStr);
            this.DigitalLinkURL = json.Value<string>("DigitalLinkURL");
            this.DID = DIDFactory.Parse(json.Value<string>("DID"));
            this.Name = json.Value<string>("Name");
            this.ID = json.Value<long>("ID");
            this.PGLN = IdentifierFactory.ParsePGLN(json.Value<string>("PGLN"), out string error);
        }
    }
}
