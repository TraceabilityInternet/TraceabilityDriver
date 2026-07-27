
namespace TraceabilityDriver.Models.Mapping;

public class CommonCatchInformation
{
    /// <summary>
    /// The gear type for the catch information.
    /// </summary>
    public string? GearType { get; set; } = null;

    /// <summary>
    /// The area where the catch took place.
    /// </summary>
    public string? CatchArea { get; set; } = null;

    /// <summary>
    /// Indicates if GPS is available for the catch information.
    /// </summary>
    public bool GPSAvailable { get; set; } = false;

    /// <summary>
    /// The economic zone where the catch took place.
    /// </summary>
    public string? EconomicZone { get; set; } = null;

    /// <summary>
    /// The fishery improvement project associated with the catch.
    /// </summary>
    public string? FisheryImprovementProject { get; set; } = null;

    /// <summary>
    /// The RFMO (Regional Fisheries Management Organization) area of the catch.
    /// </summary>
    public string? RfmoArea { get; set; } = null;

    /// <summary>
    /// The satellite tracking authority for the vessel.
    /// </summary>
    public string? SatelliteTrackingAuthority { get; set; } = null;

    /// <summary>
    /// The subnational permit area for the catch.
    /// </summary>
    public string? SubnationalPermitArea { get; set; } = null;

    /// <summary>
    /// The date of the vessel trip during which the catch took place.
    /// </summary>
    public DateTimeOffset? VesselTripDate { get; set; } = null;

    /// <summary>
    /// Merges the catch information from another object if the current object's values are not set.
    /// </summary>
    /// <param name="catchInformation">Provides information that may be used to update the current object.</param>
    public void Merge(CommonCatchInformation catchInformation)
    {
        if (this.GearType == null && catchInformation.GearType != null)
        {
            this.GearType = catchInformation.GearType;
        }

        if (this.CatchArea == null && catchInformation.CatchArea != null)
        {
            this.CatchArea = catchInformation.CatchArea;
        }

        if (!this.GPSAvailable && catchInformation.GPSAvailable)
        {
            this.GPSAvailable = catchInformation.GPSAvailable;
        }

        if (this.EconomicZone == null && catchInformation.EconomicZone != null)
        {
            this.EconomicZone = catchInformation.EconomicZone;
        }

        if (this.FisheryImprovementProject == null && catchInformation.FisheryImprovementProject != null)
        {
            this.FisheryImprovementProject = catchInformation.FisheryImprovementProject;
        }

        if (this.RfmoArea == null && catchInformation.RfmoArea != null)
        {
            this.RfmoArea = catchInformation.RfmoArea;
        }

        if (this.SatelliteTrackingAuthority == null && catchInformation.SatelliteTrackingAuthority != null)
        {
            this.SatelliteTrackingAuthority = catchInformation.SatelliteTrackingAuthority;
        }

        if (this.SubnationalPermitArea == null && catchInformation.SubnationalPermitArea != null)
        {
            this.SubnationalPermitArea = catchInformation.SubnationalPermitArea;
        }

        if (this.VesselTripDate == null && catchInformation.VesselTripDate != null)
        {
            this.VesselTripDate = catchInformation.VesselTripDate;
        }
    }
}


