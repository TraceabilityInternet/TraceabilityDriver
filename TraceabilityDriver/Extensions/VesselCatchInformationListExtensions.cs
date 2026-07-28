using OpenTraceability.GDST.Events.KDEs;
using OpenTraceability.Utility;

namespace TraceabilityDriver.Extensions
{
    public static class VesselCatchInformationListExtensions
    {
        public static void Merge(this VesselCatchInformationList? target, VesselCatchInformationList? source)
        {
            if (target == null && source != null)
            {
                target = source;
            }
            else if (target?.Vessels?.Count == 0 && source?.Vessels?.Count > 0)
            {
                target = source;
            }
            else if(target?.Vessels?.Count > 0 && source?.Vessels?.Count > 0)
            {
                // we only support a single vessel currently, so we will just merge the first one
                var targetVesselInfo = target.Vessels.First();
                var sourceVesselInfo = source.Vessels.First();

                if(targetVesselInfo.VesselFlagState == null && sourceVesselInfo.VesselFlagState != null)
                {
                    targetVesselInfo.VesselFlagState = new Country(sourceVesselInfo.VesselFlagState);
                }

                if(string.IsNullOrEmpty(targetVesselInfo.CatchArea) && !string.IsNullOrEmpty(sourceVesselInfo.CatchArea))
                {
                    targetVesselInfo.CatchArea = sourceVesselInfo.CatchArea;
                }
                if (string.IsNullOrEmpty(targetVesselInfo.EconomicZone) && !string.IsNullOrEmpty(sourceVesselInfo.EconomicZone))
                {
                    targetVesselInfo.EconomicZone = sourceVesselInfo.EconomicZone;
                }
                if (string.IsNullOrEmpty(targetVesselInfo.GearType) && !string.IsNullOrEmpty(sourceVesselInfo.GearType))
                {
                    targetVesselInfo.GearType = sourceVesselInfo.GearType;
                }
                if (string.IsNullOrEmpty(targetVesselInfo.VesselID) && !string.IsNullOrEmpty(sourceVesselInfo.VesselID))
                {
                    targetVesselInfo.VesselID = sourceVesselInfo.VesselID;
                }
                if (string.IsNullOrEmpty(targetVesselInfo.VesselName) && !string.IsNullOrEmpty(sourceVesselInfo.VesselName))
                {
                    targetVesselInfo.VesselName = sourceVesselInfo.VesselName;
                }
                if (string.IsNullOrEmpty(targetVesselInfo.FIP) && !string.IsNullOrEmpty(sourceVesselInfo.FIP))
                {
                    targetVesselInfo.FIP = sourceVesselInfo.FIP;
                }
                if (string.IsNullOrEmpty(targetVesselInfo.IMONumber) && !string.IsNullOrEmpty(sourceVesselInfo.IMONumber))
                {
                    targetVesselInfo.IMONumber = sourceVesselInfo.IMONumber;
                }
                if (string.IsNullOrEmpty(targetVesselInfo.RFMO) && !string.IsNullOrEmpty(sourceVesselInfo.RFMO))
                {
                    targetVesselInfo.RFMO = sourceVesselInfo.RFMO;
                }
                if (string.IsNullOrEmpty(targetVesselInfo.SatelliteTrackingAuthority) && !string.IsNullOrEmpty(sourceVesselInfo.SatelliteTrackingAuthority))
                {
                    targetVesselInfo.SatelliteTrackingAuthority = sourceVesselInfo.SatelliteTrackingAuthority;
                }
                if (string.IsNullOrEmpty(targetVesselInfo.SubNationalPermitArea) && !string.IsNullOrEmpty(sourceVesselInfo.SubNationalPermitArea))
                {
                    targetVesselInfo.SubNationalPermitArea = sourceVesselInfo.SubNationalPermitArea;
                }
                if (string.IsNullOrEmpty(targetVesselInfo.VesselPublicRegistry) && !string.IsNullOrEmpty(sourceVesselInfo.VesselPublicRegistry))
                {
                    targetVesselInfo.VesselPublicRegistry = sourceVesselInfo.VesselPublicRegistry;
                }
                if (targetVesselInfo.VesselTripDate == null && sourceVesselInfo.VesselTripDate != null)
                {
                    targetVesselInfo.VesselTripDate = sourceVesselInfo.VesselTripDate;
                }

                // We don't handle extra KDEs as they aren't mapped by the driver anyway
            }
        }
    }
}
