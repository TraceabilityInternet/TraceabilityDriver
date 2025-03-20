using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Models.Driver;

namespace TraceabilityEngine.ServiceUtil.Converters
{
    public class ITEDriverTradingPartnerConverter : JsonConverter<ITEDriverTradingPartner>
    {
        public override ITEDriverTradingPartner ReadJson(JsonReader reader, Type objectType, [AllowNull] ITEDriverTradingPartner existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            ITEDriverTradingPartner account = TEDriverFactory.CreateTradingPartner(json.ToString());
            return account;
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] ITEDriverTradingPartner value, JsonSerializer serializer)
        {
            if (value != null)
            {
                JObject json = JObject.Parse(value.ToJson());
                json.WriteTo(writer);
            }
        }
    }
}
