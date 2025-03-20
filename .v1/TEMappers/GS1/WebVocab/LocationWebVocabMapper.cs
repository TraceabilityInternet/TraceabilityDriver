using TraceabilityEngine.Util;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Models.Locations;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Models;
using TraceabilityEngine.StaticData;

namespace TraceabilityEngine.Mappers
{
    public class LocationWebVocabMapper : GS1WebVocabMapper, ITELocationMapper
    {
        public ITELocation ConvertToLocation(string jsonStr)
        {
            try
            {
                JObject json = JObject.Parse(jsonStr);

                TELocation location = new TELocation();
                location.Name = GetLanguageString(json, "name");
                location.Description = GetLanguageString(json, "description");
                location.GLN = IdentifierFactory.ParseGLN(GetStringValue(json, "globalLocationNumber"));
                location.LocationWebURI = GetStringValue(json, "@id"); //Check location value on MDS

                location.Address = GetAddress(json, "address");

                GetGeo(json, location);

                location.Contacts = GetContacts(json, "contactPoint");
                location.Certificates = GetCertificates(json, "certification");
                location.Photos = GetPhotos(json, "image");

                location.Farm = GetFarmInformation(json, "te:farmInformation");
                location.Vessel = GetVesselInformation(json, "te:vesselInformation");
                
                return location;
            }
            catch (Exception Ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                TELogger.Log(0, Ex);
                throw;
            }
        }
        public string ConvertFromLocation(ITELocation location)
        {
            try
            {
                if (location == null) throw new ArgumentNullException(nameof(location));
                JObject json = new JObject();
                SetContext(json);
                SetString(json, "@type", "gs1:Place");
                SetString(json, "globalLocationNumber", location.GLN.ToString());
                SetString(json, "@id", location.LocationWebURI);
                SetLanguageString(json, "name", location.Name);
                SetLanguageString(json, "description", location.Description);
                SetGeo(json, location);
                SetAddress(json, "address", location.Address);
                SetContacts(json, location.Contacts);
                SetCertificates(json, location.Certificates);
                SetPhotos(json, "image", location.Photos);
                SetVesselInformation(json, "te:vesselInformation", location.Vessel);
                SetFarmInformation(json, "te:farmInformation", location.Farm);
                return json.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception Ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static void SetGeo(JObject json, ITELocation location)
        {
            if (location.Longitude != 0)
            {
                JObject jGeo = new JObject();
                SetDouble(jGeo, "latitude", location.Latitude);
                SetDouble(jGeo, "longitude", location.Longitude);
                SetString(jGeo, "@type", "gs1:GeoCoordinates");
                json["geo"] = jGeo;
            }
        }

        public static void GetGeo(JObject json, ITELocation location)
        {
            if (json?["geo"] != null && json?["geo"] is JObject)
            {
                JObject jGeo = json["geo"] as JObject;
                location.Latitude = GetDoubleValue(jGeo, "latitude");
                location.Longitude = GetDoubleValue(jGeo, "longitude");
            }
        }

        public static void SetFarmInformation(JObject json, string propertyName, ITEFarmInformation farm)
        {
            if (json != null && farm != null)
            {
                JObject jFarm = new JObject();
                SetString(jFarm, "te:farmNumber", farm.FarmNumber);
                SetString(jFarm, "te:permitNumber", farm.PermitNumber);
                json[propertyName] = jFarm;
            }
        }
        public static void SetVesselInformation(JObject json, string propertyName, ITEVesselInformation vessel)
        {
            if (json != null && vessel != null)
            {
                JObject jVessel = new JObject();
                SetString(jVessel, "te:imoNumber", vessel.IMONumber);
                SetString(jVessel, "te:rmfoNumber", vessel.RMFONumber);
                SetString(jVessel, "te:vesselCallSign", vessel.VesselCallSign);
                SetCountry(jVessel, "te:vesselFlag", vessel.VesselFlag);
                json[propertyName] = jVessel;
            }
        }

        public static ITEFarmInformation GetFarmInformation(JObject json, string propertyName)
        {
            ITEFarmInformation farm = null;
            if (json?[propertyName] != null && json?[propertyName] is JObject)
            {
                farm = new TEFarmInformation();

                JObject jFarm = json[propertyName] as JObject;
                farm.FarmNumber = GetStringValue(jFarm, "te:farmNumber");
                farm.PermitNumber = GetStringValue(jFarm, "te:permitNumber");
            }
            return farm;
        }
        public static ITEVesselInformation GetVesselInformation(JObject json, string propertyName)
        {
            ITEVesselInformation vessel = null;
            if (json?[propertyName] != null && json?[propertyName] is JObject)
            {
                vessel = new TEVesselInformation();

                JObject jVessel = json[propertyName] as JObject;
                vessel.IMONumber = GetStringValue(jVessel, "te:imoNumber");
                vessel.RMFONumber = GetStringValue(jVessel, "te:rmfoNumber");
                vessel.VesselCallSign = GetStringValue(jVessel, "te:vesselCallSign");
                vessel.VesselFlag = GetCountry(jVessel, "te:vesselFlag");
            }
            return vessel;
        }
    }
}
