using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Services.DirectoryService;

namespace TraceabilityEngine.Models.Directory
{
    public class TEDirectoryAdmin : ITEDirectoryAdmin
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ObjectID { get; set; }

        public string Email { get; set; }
        public string EncryptedPassword { get; set; }
    }
}
