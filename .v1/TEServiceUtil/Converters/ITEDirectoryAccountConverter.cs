using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;

namespace TraceabilityEngine.ServiceUtil.Converters
{
    public class ITEDirectoryAccountConverter : JsonConverter<ITEDirectoryAccount>
    {
        public override ITEDirectoryAccount ReadJson(JsonReader reader, Type objectType, [AllowNull] ITEDirectoryAccount existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            ITEDirectoryAccount account = new TEDirectoryAccount();
            JObject json = JObject.Load(reader);
            account.FromJson(json.ToString());
            return account;
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] ITEDirectoryAccount value, JsonSerializer serializer)
        {
            if (value != null)
            {
                JObject json = JObject.Parse(value.ToJson());
                json.WriteTo(writer);
            }
        }
    }
}
