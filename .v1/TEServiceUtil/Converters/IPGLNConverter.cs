using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityEngine.ServiceUtil.Converters
{
    public class IPGLNConverter : JsonConverter<IPGLN>
    {
        public override IPGLN ReadJson(JsonReader reader, Type objectType, [AllowNull] IPGLN existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string json = reader.ReadAsString();
            IPGLN pgln = IdentifierFactory.ParsePGLN(json);
            return pgln;
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] IPGLN value, JsonSerializer serializer)
        {
            if (value != null)
            {
                writer.WriteValue(value.ToString());
            }
        }
    }
}
