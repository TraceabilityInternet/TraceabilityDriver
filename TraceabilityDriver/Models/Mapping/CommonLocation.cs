using OpenTraceability.Models.Identifiers;
using OpenTraceability.Utility;

namespace TraceabilityDriver.Models.Mapping;

public class CommonLocation : CommonBaseModel
{
    /// <summary>
    /// A unique identifier for the location.
    /// </summary>
    public string? LocationId { get; set; } = null;

    /// <summary>
    /// The ID of the owner of the location.
    /// </summary>
    public string? OwnerId { get; set; } = null;

    /// <summary>
    /// The registration number for the location.
    /// </summary>
    public string? RegistrationNumber { get; set; } = null;

    /// <summary>
    /// The name of the location.
    /// </summary>
    public string? Name { get; set; } = null;

    /// <summary>
    /// The country of the location.
    /// </summary>
    public Country? Country { get; set; } = null;

    /// <summary>
    /// A comma-delimited list of GDST location classification values for the location (e.g. "vessel" or "land facility").
    /// </summary>
    public string? LocationClassification { get; set; } = null;

    /// <summary>
    /// The vessel identifier for vessel locations (e.g. "VESSEL1").
    /// </summary>
    public string? VesselId { get; set; } = null;

    /// <summary>
    /// The IMO number for vessel locations.
    /// </summary>
    public string? ImoNumber { get; set; } = null;

    /// <summary>
    /// The public registry URL for vessel locations.
    /// </summary>
    public string? VesselPublicRegistry { get; set; } = null;

    /// <summary>
    /// The flag state of the vessel for vessel locations.
    /// </summary>
    public Country? VesselFlagState { get; set; } = null;

    /// <summary>
    /// The first street address line of the location.
    /// </summary>
    public string? Address1 { get; set; } = null;

    /// <summary>
    /// The second street address line of the location.
    /// </summary>
    public string? Address2 { get; set; } = null;

    /// <summary>
    /// The city of the location.
    /// </summary>
    public string? City { get; set; } = null;

    /// <summary>
    /// The state of the location.
    /// </summary>
    public string? State { get; set; } = null;

    /// <summary>
    /// The postal code of the location.
    /// </summary>
    public string? PostalCode { get; set; } = null;

    /// <summary>
    /// The geo location URI of the location (e.g. "geo:37.7749,-122.4194").
    /// </summary>
    public string? GeoLocation { get; set; } = null;

    /// <summary>
    /// The geo fence of the location as a JSON array of coordinate pairs.
    /// </summary>
    public string? GeoFence { get; set; } = null;

    /// <summary>
    /// Merges the location onto this one. Properties are only merged if they are null.
    /// </summary>
    /// <param name="other">The other location.</param>
    public void Merge(CommonLocation other)
    {
        if (this.LocationId == null && other.LocationId != null)
        {
            this.LocationId = other.LocationId;
        }

        if (this.RegistrationNumber == null && other.RegistrationNumber != null)
        {
            this.RegistrationNumber = other.RegistrationNumber;
        }

        if (this.Name == null && other.Name != null)
        {
            this.Name = other.Name;
        }

        if (this.Country == null && other.Country != null)
        {
            this.Country = other.Country;
        }

        if (this.LocationClassification == null && other.LocationClassification != null)
        {
            this.LocationClassification = other.LocationClassification;
        }

        if (this.VesselId == null && other.VesselId != null)
        {
            this.VesselId = other.VesselId;
        }

        if (this.ImoNumber == null && other.ImoNumber != null)
        {
            this.ImoNumber = other.ImoNumber;
        }

        if (this.VesselPublicRegistry == null && other.VesselPublicRegistry != null)
        {
            this.VesselPublicRegistry = other.VesselPublicRegistry;
        }

        if (this.VesselFlagState == null && other.VesselFlagState != null)
        {
            this.VesselFlagState = other.VesselFlagState;
        }

        if (this.Address1 == null && other.Address1 != null)
        {
            this.Address1 = other.Address1;
        }

        if (this.Address2 == null && other.Address2 != null)
        {
            this.Address2 = other.Address2;
        }

        if (this.City == null && other.City != null)
        {
            this.City = other.City;
        }

        if (this.State == null && other.State != null)
        {
            this.State = other.State;
        }

        if (this.PostalCode == null && other.PostalCode != null)
        {
            this.PostalCode = other.PostalCode;
        }

        if (this.GeoLocation == null && other.GeoLocation != null)
        {
            this.GeoLocation = other.GeoLocation;
        }

        if (this.GeoFence == null && other.GeoFence != null)
        {
            this.GeoFence = other.GeoFence;
        }
    }

    /// <summary>
    /// This method will attempt to generate a GTIN for the product definition.
    /// </summary>
    /// <returns></returns>
    public GLN GetGLN()
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(this.LocationId);

        // If the location ID is already a GLN, then just parse that and return it.
        if (GLN.IsGLN(this.LocationId))
        {
            return new GLN(this.LocationId);
        }
        else
        {
            // Ensure we have an owner ID set.
            ArgumentNullException.ThrowIfNullOrWhiteSpace(this.OwnerId);

            // Generate the GTIN.
            string gln = $"urn:gdst:{GDST_IDENTIFIERS_DOMAIN}:location:loc:{NormalizeString(this.OwnerId)}.{NormalizeString(this.LocationId)}";
            return new GLN(gln);
        }
    }
}


