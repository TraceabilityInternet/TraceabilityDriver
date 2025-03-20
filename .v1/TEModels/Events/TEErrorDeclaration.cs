using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Models.Events
{
    public class TEErrorDeclaration : ITEErrorDeclaration
    {
        public string RawReason { get; set; }
        public DateTime? DeclarationTime { get; set; }
        public List<string> CorrectingEventIDs { get; set; }
        public TEEventErrorType Reason
        {
            get
            {
                TEEventErrorType type = TEEventErrorType.Unknown;
                foreach (TEEventErrorType t in Enum.GetValues(typeof(TEEventErrorType)))
                {
                    if (TEEnumUtil.GetEnumKey(t) == RawReason)
                    {
                        type = t;
                    }
                }
                return type;
            }
            set
            {
                string reason = TEEnumUtil.GetEnumKey(value);
                this.RawReason = reason;
            }
        }
    }
}
