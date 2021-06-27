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
    public class ITEDirectoryNewAccountConverter : JsonConverter<ITEDirectoryNewAccount>
    {
        public override ITEDirectoryNewAccount ReadJson(JsonReader reader, Type objectType, [AllowNull] ITEDirectoryNewAccount existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            ITEDirectoryNewAccount account = new TEDirectoryNewAccount();
            JObject json = JObject.Load(reader);
            account.FromJson(json.ToString());
            return account;
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] ITEDirectoryNewAccount value, JsonSerializer serializer)
        {
            if (value != null)
            {
                JObject json = JObject.Parse(value.ToJson());
                json.WriteTo(writer);
            }
        }
    }
}
