using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Models.Events
{
    public class TEEventSource : ITEEventSource
    {
        public string RawType { get; set; }

        public TEEventSourceType Type
        {
            get
            {
                TEEventSourceType type = TEEventSourceType.Unknown;
                foreach (TEEventSourceType t in Enum.GetValues(typeof(TEEventSourceType)))
                {
                    if (TEEnumUtil.GetEnumKey(t) == RawType)
                    {
                        type = t;
                    }
                }
                return type;
            }
        }

        public string Value { get; set; }

        public TEEventSource()
        {

        }

        public TEEventSource(IPGLN pgln, TEEventSourceType type)
        {
            if (type != TEEventSourceType.Owner && type != TEEventSourceType.Possessor)
            {
                throw new TEException("When constructing a TEEventSource from a PGLN, the TEEventSourceType must either be Owner or Possessor.");
            }

            this.RawType = TEEnumUtil.GetEnumKey(type);
            this.Value = pgln.ToString();
        }
    }
}
