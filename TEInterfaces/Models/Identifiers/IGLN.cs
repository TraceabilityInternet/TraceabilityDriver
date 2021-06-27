using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityEngine.Interfaces.Models.Identifiers
{
    public interface IGLN : IBsonSerializer
    {
        string ToDigitalLinkURL(string baseURL);
    }
}
