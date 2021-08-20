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
        public IPublicDID PublicDID { get; set; }
        public string APIAccessKey { get; set; }
        public TradingPartnerCommunicationProtocol Protocol { get; set; }

        public TEDriverTradingPartner()
        {

        }

        public TEDriverTradingPartner(ITEDriverAccount account)
        {
            this.Name = account.Name;
            this.PGLN = account.PGLN;
            this.DigitalLinkURL = account.DigitalLinkURL;
            this.PublicDID = account.DID.ToPublicDID();
        }

        public string ToJson()
        {
            JObject json = new JObject();
            json["DigitalLinkURL"] = this.DigitalLinkURL;
            json["PublicDID"] = this.PublicDID?.ToString();
            json["Name"] = this.Name;
            json["AccountID"] = this.AccountID;
            json["ID"] = this.ID;
            json["PGLN"] = this.PGLN?.ToString();
            json["Protocol"] = this.Protocol.ToString();
            return json.ToString();
        }

        public void FromJson(string jsonStr)
        {
            JObject json = JObject.Parse(jsonStr);
            this.DigitalLinkURL = json.Value<string>("DigitalLinkURL");
            this.PublicDID = DIDFactory.ParsePublic(json.Value<string>("PublicDID"));
            this.Name = json.Value<string>("Name");
            this.ID = json.Value<long>("ID");
            this.AccountID = json.Value<long>("AccountID");
            this.PGLN = IdentifierFactory.ParsePGLN(json.Value<string>("PGLN"), out string error);
            if(Enum.TryParse<TradingPartnerCommunicationProtocol>(json.Value<string>("Protocol"), out TradingPartnerCommunicationProtocol protocol))
            {
                this.Protocol = protocol;
            }
            else
            {
                this.Protocol = TradingPartnerCommunicationProtocol.Classic;
            }
        }
    }
}
