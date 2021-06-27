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
    public class TEEventDestination : ITEEventDestination
    {
        public string RawType { get; set; }

        public TEEventDestinationType Type
        {
            get
            {
                TEEventDestinationType type = TEEventDestinationType.Unknown;
                foreach (TEEventDestinationType t in Enum.GetValues(typeof(TEEventDestinationType)))
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

        public TEEventDestination()
        {

        }

        public TEEventDestination(IPGLN pgln, TEEventDestinationType type)
        {
            if (type != TEEventDestinationType.Owner && type != TEEventDestinationType.Possessor)
            {
                throw new TEException("When constructing a TEEventDestination from a PGLN, the TEEventDestinationType must either be Owner or Possessor.");
            }

            this.RawType = TEEnumUtil.GetEnumKey(type);
            this.Value = pgln.ToString();
        }
    }
}
