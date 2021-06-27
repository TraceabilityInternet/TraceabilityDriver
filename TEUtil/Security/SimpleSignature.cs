using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Util.Security
{
    public class SimpleSignature : ISimpleSignature
    {
        public string Value { get; set; }
        public string Nunce { get; set; }
        public string Signature { get; set; }

        public override string ToString()
        {
            JObject json = new JObject();
            json["Value"] = Value;
            json["Nunce"] = Nunce;
            json["Signature"] = Signature;
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json.ToString()));
            return "urn:traceability_engine:simplesignature_v1:" + base64;
        }
        public void Parse(string strValue)
        {
            strValue = strValue.Replace("urn:traceability_engine:simplesignature_v1:", "");
            string jsonStr = Encoding.UTF8.GetString(Convert.FromBase64String(strValue));
            JObject json = JObject.Parse(jsonStr);
            Value = json.Value<string>("Value");
            Nunce = json.Value<string>("Nunce");
            Signature = json.Value<string>("Signature");
        }
    } 
}
