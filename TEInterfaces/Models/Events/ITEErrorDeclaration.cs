using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Interfaces.Models.Events
{
    public enum TEEventErrorType
    {
        Unknown = 0,

        [TEKey("urn:epcis:errorType:incorrect_data")]
        IncorrectData = 1,

        [TEKey("urn:epcis:errorType:did_not_occur")]
        DidNotOccur = 2
    }

    public interface ITEErrorDeclaration
    {
        TEEventErrorType Reason { get; set; }
        string RawReason { get; set; }
        DateTime? DeclarationTime { get; set; }
        List<string> CorrectingEventIDs { get; set; }
    }
}
