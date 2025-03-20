using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Clients
{
    public static class TEClientFactory
    {
        public static ITEDirectoryClient DirectoryClient(IDID did, string url)
        {
            ITEDirectoryClient client = new TEDirectoryClient(did, url);
            return client;
        }

        public static ITEInternalClient InternalClient(string url, string apiKey)
        {
            return new TETraceabilityDriverClient(url, apiKey);
        }
    }
}
