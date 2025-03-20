using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.DigitalLink;
using TraceabilityEngine.Models.DigitalLink;
using TraceabilityEngine.Util;

namespace TraceabilityDriverService
{
    public static class TraceabilityDriverServiceFactory
    {
        public static ITETraceabilityMapper LoadMapper(string dllPath, string className)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(dllPath);
                Assembly asm = Assembly.Load(bytes);
                Type type = asm.GetType(className);
                if (type.IsAssignableTo(typeof(ITETraceabilityMapper)))
                {
                    ITETraceabilityMapper driver = (ITETraceabilityMapper)Activator.CreateInstance(type);
                    return driver;
                }
                else
                {
                    throw new TEException($"The type ${type.FullName} does not implement the ITETraceabilityDriver interface.");
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static List<ITEDigitalLink> ParseLinks(string json)
        {
            List<TEDigitalLink> links = JsonConvert.DeserializeObject<List<TEDigitalLink>>(json);
            return links.Select(l => (ITEDigitalLink)l).ToList();
        }
    }
}
