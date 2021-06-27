using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Models.Directory
{
    [BsonDiscriminator("TEDirectoryServiceProvider")]
    public class TEDirectoryServiceProvider : ITEDirectoryServiceProvider
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ObjectID { get; set; }

        public IDID DID { get; set; }

        public string Name { get; set; }

        public IPGLN PGLN { get; set; }
    }
}
