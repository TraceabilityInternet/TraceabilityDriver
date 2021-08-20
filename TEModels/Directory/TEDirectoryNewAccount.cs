using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityEngine.Models.Directory
{
    [BsonDiscriminator("TEDirectoryNewAccount")]
    public class TEDirectoryNewAccount : TEDirectoryAccount, ITEDirectoryNewAccount
    {
        public ISimpleSignature AccountSignature { get; set; }
        public ISimpleSignature ServiceProviderSignature { get; set; }
        public IDID ServiceProviderDID { get; set; }
        public IDID DID { get; set; }

        public ITEDirectoryAccount ToAccount()
        {
            ITEDirectoryAccount account = new TEDirectoryAccount();
            account.ID = this.ID;
            account.PublicDID = this.DID.ToPublicDID();
            account.DigitalLinkURL = this.DigitalLinkURL;
            account.PGLN = this.PGLN;
            account.ServiceProviderPGLN = this.ServiceProviderPGLN;
            return account;
        }
        public override void FromJson(string jsonStr)
        {
            base.FromJson(jsonStr);
            JObject json = JObject.Parse(jsonStr);

            this.AccountSignature = SimpleSignatureFactory.Parse(json.Value<string>("AccountSignature"));
            this.ServiceProviderSignature = SimpleSignatureFactory.Parse(json.Value<string>("ServiceProviderSignature"));
            this.ServiceProviderPGLN = IdentifierFactory.ParsePGLN(json.Value<string>("ServiceProviderPGLN"), out string error);
            this.DID = DIDFactory.Parse(json.Value<string>("DID"));
        }
        public override string ToJson()
        {
            string jsonStr = base.ToJson();
            JObject json = JObject.Parse(jsonStr);

            json["AccountSignature"] = this.AccountSignature?.ToString();
            json["ServiceProviderSignature"] = this.ServiceProviderSignature?.ToString();
            json["ServiceProviderPGLN"] = this.ServiceProviderPGLN?.ToString();
            json["DID"] = this.DID?.ToString();

            return json.ToString();
        }
        public bool VerifySignature()
        {
            if (AccountSignature == null)
            {
                return false;
            }

            if (AccountSignature.Value != this.ServiceProviderSignature.Value)
            {
                return false;
            }

            string[] parts = AccountSignature.Value.Split("|");
            if (parts.Length != 2)
            {
                return false;
            }

            if (parts[0] != this.PGLN?.ToString())
            {
                return false;
            }

            if (parts[1] != this.ServiceProviderPGLN?.ToString())
            {
                return false;
            }

            if (IDID.IsNullOrEmpty(this.PublicDID))
            {
                return false;
            }

            if (!this.PublicDID.Verify(this.AccountSignature))
            {
                return false;
            }

            if (!this.ServiceProviderDID.Verify(this.ServiceProviderSignature))
            {
                return false;
            }

            return true;
        }

        public void Sign()
        {
            if (IPGLN.IsNullOrEmpty(this.PGLN))
            {
                throw new NullReferenceException("PGLN is null or empty.");
            }

            if (IDID.IsNullOrEmpty(this.PublicDID))
            {
                throw new NullReferenceException("DID is null or empty.");
            }

            if (IDID.IsNullOrEmpty(this.ServiceProviderDID))
            {
                throw new NullReferenceException("Requester is null or empty.");
            }

            // combine the pgln of the service provider and the account to make the signature value
            string value = $"{this.PGLN?.ToString()}|{this.ServiceProviderPGLN?.ToString()}";

            this.AccountSignature = this.DID.Sign(value, DateTime.UtcNow.ToString());
            this.ServiceProviderSignature = this.ServiceProviderDID.Sign(value, DateTime.UtcNow.ToString());
        }
    }
}
