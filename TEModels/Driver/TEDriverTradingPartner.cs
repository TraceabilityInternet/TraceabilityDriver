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
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityEngine.Models.Driver
{
    public class TEDriverTradingPartner : ITEDriverTradingPartner
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ObjectID { get; set; }


        public long ID { get; set; }
        public long AccountID { get; set; }
        public string Name { get; set; }
        public IPGLN PGLN { get; set; }
        public string DigitalLinkURL { get; set; }
        public IDID DID { get; set; }
        public string RequestingAPIKey { get; set; }
        public string ReceivingAPIKey { get; set; }

        public string ToJson()
        {
            JObject json = new JObject();
            json["DigitalLinkURL"] = this.DigitalLinkURL;
            json["DID"] = this.DID?.ToString();
            json["Name"] = this.Name;
            json["AccountID"] = this.AccountID;
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
            this.AccountID = json.Value<long>("AccountID");
            this.PGLN = IdentifierFactory.ParsePGLN(json.Value<string>("PGLN"), out string error);
        }
    }
}
