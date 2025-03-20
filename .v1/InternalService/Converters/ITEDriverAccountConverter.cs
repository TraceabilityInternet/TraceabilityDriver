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

namespace InternalService.Converters
{
    public class ITEDriverAccountConverter : JsonConverter<ITEDriverAccount>
    {
        public override ITEDriverAccount ReadJson(JsonReader reader, Type objectType, [AllowNull] ITEDriverAccount existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            ITEDriverAccount account = TEDriverFactory.CreateAccount(json.ToString());
            return account;
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] ITEDriverAccount value, JsonSerializer serializer)
        {
            if (value != null)
            {
                JObject json = JObject.Parse(value.ToJson());
                json.WriteTo(writer);
            }
        }
    }
}
