using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.DigitalLink;

namespace TraceabilityEngine.Models.DigitalLink
{
    public class TEDigitalLink : ITEDigitalLink
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ObjectID { get; set; }


        public string link { get; set; }
        public string title { get; set; }
        public string linkType { get; set; }
        public string ianaLanguage { get; set; }
        public string context { get; set; }
        public string mimeType { get; set; }
        public bool active { get; set; }
        public bool fwqs { get; set; }
        public bool defaultLinkType { get; set; }
        public bool defaultIanaLanguage { get; set; }
        public bool defaultContext { get; set; }

        public bool defaultMimeType { get; set; }

        public string identifier { get; set; }
        public bool authRequired { get; set; }
    }
}
