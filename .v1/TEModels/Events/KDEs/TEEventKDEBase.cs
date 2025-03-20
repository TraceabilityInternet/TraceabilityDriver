using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Models.Events.KDEs
{
    public class TEEventKDEBase
    {
        public string Namespace { get; set; }
        public string NamespacePrefix { get; set; }
        public string Name { get; set; }
    }
}
