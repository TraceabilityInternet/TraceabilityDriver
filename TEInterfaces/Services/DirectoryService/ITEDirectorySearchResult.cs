using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Interfaces.Services.DirectoryService
{
    public interface ITEDirectorySearchResult
    {
        public string Name { get; set; }
        public IPGLN PGLN { get; set; }
        public string ServiceProviderName { get; set; }
        public IPGLN ServiceProviderPGLN { get; set; }
    }
}
