
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
    /// Merges gear type, catch area, and GPS availability information from another object if the current object's values are not set.
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
    }
}


