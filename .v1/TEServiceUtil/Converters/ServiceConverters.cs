using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.ServiceUtil.Converters
{
    public static class ServiceConverters
    {
        public static List<Type> CommonConverters()
        {
            List<Type> types = new List<Type>();
            types.Add(typeof(ITEDirectoryNewAccountConverter));
            types.Add(typeof(ITEDirectoryAccountConverter));
            types.Add(typeof(IDIDConverter));
            types.Add(typeof(IPGLNConverter));
            types.Add(typeof(ITEDriverAccountConverter));
            types.Add(typeof(ITEDriverTradingPartnerConverter));
            return types;
        }
    }
}
