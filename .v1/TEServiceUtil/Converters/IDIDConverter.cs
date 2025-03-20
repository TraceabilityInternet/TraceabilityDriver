using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityEngine.ServiceUtil.Converters
{
    public class IDIDConverter : JsonConverter<IDID>
    {
        public override IDID ReadJson(JsonReader reader, Type objectType, [AllowNull] IDID existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string json = reader.ReadAsString();
            IDID did = DIDFactory.Parse(json);
            return did;
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] IDID value, JsonSerializer serializer)
        {
            if (value != null)
            {
                writer.WriteValue(value.ToString());
            }
        }
    }
}
