using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Interfaces.Models.Locations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TraceabilityEngine.Models.Locations
{
    public class TELocation : TELocationLite, ITELocation
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ObjectID { get; set; }

        public string LocationWebURI { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public ITEAddress Address { get; set; } = new TEAddress();

        public ITEFarmInformation Farm { get; set; } = new TEFarmInformation();

        public ITEVesselInformation Vessel { get; set; } = new TEVesselInformation();

        public List<ITECertificate> Certificates { get; set; } = new List<ITECertificate>();

        public List<ITEPhoto> Photos { get; set; } = new List<ITEPhoto>();

        public List<ITEContact> Contacts { get; set; } = new List<ITEContact>();
    }
}
