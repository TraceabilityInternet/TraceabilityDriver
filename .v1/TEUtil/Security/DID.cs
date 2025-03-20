using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Util.Security
{
    public class DID : PublicDID, IDID
    {
        public static DID GenerateNew()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            string publicKey = rsa.ToXmlString(false);
            string privateKey = rsa.ToXmlString(true);
            string id = "did:traceabilityengine_v1:" + Guid.NewGuid().ToString();

            DID did = new DID(id, publicKey, privateKey);
            return did;
        }

        public string PrivateKey { get; set; }


        public DID()
        {

        }

        public DID(string id, string publicKey)
        {
            this.ID = id;
            this.PublicKey = publicKey;
        }

        public DID(string id, string publicKey, string privateKey)
        {
            this.ID = id;
            this.PublicKey = publicKey;
            this.PrivateKey = privateKey;
        }

        public ISimpleSignature Sign(string value, string nunce)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.PrivateKey))
                {
                    throw new NullReferenceException(this.PrivateKey);
                }

                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(this.PrivateKey);

                byte[] data = Encoding.UTF8.GetBytes(value + nunce);
                byte[] signatureData = rsa.SignData(data, CryptoConfig.MapNameToOID("SHA512"));
                string signature = Convert.ToBase64String(signatureData);

                ISimpleSignature simpleSignature = new SimpleSignature();
                simpleSignature.Value = value;
                simpleSignature.Nunce = nunce;
                simpleSignature.Signature = signature;
                return simpleSignature;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public new void Parse(string strValue)
        {
            if (string.IsNullOrWhiteSpace(strValue))
            {
                throw new ArgumentNullException(nameof(strValue));
            }

            string[] parts = strValue.Split(".");
            this.ID = parts[0];

            string jsonStr = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
            JObject jKey = JObject.Parse(jsonStr);
            this.PublicKey = Encoding.UTF8.GetString(Convert.FromBase64String(jKey.Value<string>("PublicKey")));
            this.PrivateKey = Encoding.UTF8.GetString(Convert.FromBase64String(jKey.Value<string>("PrivateKey")));
        }

        public new string ToString()
        {
            JObject jKey = new JObject();
            jKey["PublicKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(PublicKey ?? ""));
            jKey["PrivateKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(PrivateKey ?? ""));
            string jsonStr = jKey.ToString();

            string strValue = ID + "." + Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonStr));
            return strValue;
        }

        public IPublicDID ToPublicDID()
        {
            return (IPublicDID)this;
        }
    }
}
