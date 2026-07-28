using OpenTraceability.GDST.Events.KDEs;
using OpenTraceability.Utility;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for the vessel catch information list of a GDST ILMD.
    /// </summary>
    public static class VesselCatchInformationListExtensions
    {
        /// <summary>
        /// Merges the source vessel catch information into the target list.
        /// </summary>
        /// <remarks>
        /// An empty target takes the source's vessels outright. Otherwise only the first vessel of each list
        /// is merged, because the driver maps a single vessel per event; the target's values win and the
        /// source only fills what the target is missing. Extra KDEs are not merged - the driver does not map
        /// them.
        /// </remarks>
        /// <param name="target">The list to merge into. Modified in place.</param>
        /// <param name="source">The list to merge from. Not modified.</param>
        public static void Merge(this VesselCatchInformationList target, VesselCatchInformationList? source)
        {
            if (source?.Vessels == null || source.Vessels.Count == 0)
            {
                return;
            }

            // The list itself cannot be replaced from inside an extension method, so an empty target is
            // filled by copying the source's vessels into it rather than by reassigning.
            if (target.Vessels.Count == 0)
            {
                target.Vessels.AddRange(source.Vessels);
                return;
            }

            var targetVesselInfo = target.Vessels.First();
            var sourceVesselInfo = source.Vessels.First();

            if (targetVesselInfo.VesselFlagState == null && sourceVesselInfo.VesselFlagState != null)
            {
                targetVesselInfo.VesselFlagState = new Country(sourceVesselInfo.VesselFlagState);
            }

            if (string.IsNullOrEmpty(targetVesselInfo.CatchArea) && !string.IsNullOrEmpty(sourceVesselInfo.CatchArea))
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
        }
    }
}
