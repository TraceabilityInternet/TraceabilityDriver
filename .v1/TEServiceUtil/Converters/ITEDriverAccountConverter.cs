using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Models.Driver;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.ServiceUtil.Converters
{
    public class ITEDriverAccountConverter : JsonConverter<ITEDriverAccount>
    {
        public override ITEDriverAccount ReadJson(JsonReader reader, Type objectType, [AllowNull] ITEDriverAccount existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                JObject json = JObject.Load(reader);
                ITEDriverAccount account = TEDriverFactory.CreateAccount(json.ToString());
                return account;
            }
            catch (Exception Ex)
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
#endif
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] ITEDriverAccount value, JsonSerializer serializer)
        {
            try
            {
                if (value != null)
                {
                    JObject json = JObject.Parse(value.ToJson());
                    json.WriteTo(writer);
                }
            }
            catch (Exception Ex)
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
#endif
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
