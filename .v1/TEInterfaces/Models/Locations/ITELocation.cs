using TraceabilityEngine.Interfaces.Models.Common;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.DB.DocumentDB;

namespace TraceabilityEngine.Interfaces.Models.Locations
{
    public interface ITELocation : ITELocationLite, ITEDocumentObject
    {
        string LocationWebURI { get; set; }
        double Latitude { get; set; }
        double Longitude { get; set; }
        ITEAddress Address { get; set; }
        ITEFarmInformation Farm { get; set; }
        ITEVesselInformation Vessel { get; set; }
        List<ITECertificate> Certificates { get; set; }
        List<ITEPhoto> Photos { get; set; }
        List<ITEContact> Contacts { get; set; }
    }
}
