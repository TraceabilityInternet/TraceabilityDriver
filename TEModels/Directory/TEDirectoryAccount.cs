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
using TraceabilityEngine.Models.Driver;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityEngine.Models.Directory
{
    [BsonDiscriminator("TEDirectoryAccount")]
    public class TEDirectoryAccount : ITEDirectoryAccount
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ObjectID { get; set; }

        public TEDirectoryAccount()
        {

        }

        public long ID { get; set; }
        public string Name { get; set; }
        public IPGLN ServiceProviderPGLN { get; set; }
        public IPGLN PGLN { get; set; }
        public IDID DID { get; set; }
        public string DigitalLinkURL { get; set; }

        public virtual string ToJson()
        {
            JObject json = new JObject();
            json["PGLN"] = this.PGLN?.ToString();
            json["DID"] = this.DID?.ToString();
            json["DigitalLinkURL"] = this.DigitalLinkURL;
            return json.ToString();
        }
        public virtual void FromJson(string jsonStr)
        {
            if (string.IsNullOrWhiteSpace(jsonStr))
            {
                throw new ArgumentNullException(nameof(jsonStr));
            }

            JObject json = JObject.Parse(jsonStr);

            this.PGLN = IdentifierFactory.ParsePGLN(json.Value<string>("PGLN"), out string error);

            string didStr = json.Value<string>("DID");
            if (!string.IsNullOrWhiteSpace(didStr))
            {
                IDID did = DIDFactory.Parse(didStr);
                this.DID = did;
            }

            this.DigitalLinkURL = json.Value<string>("DigitalLinkURL");
        }

        public ITEDriverTradingPartner ToDriverTradingPartner()
        {
            ITEDriverTradingPartner tp = new TEDriverTradingPartner();
            tp.DID = this.DID;
            tp.PGLN = this.PGLN;
            tp.DigitalLinkURL = this.DigitalLinkURL;
            return tp;
        }
    }
}
