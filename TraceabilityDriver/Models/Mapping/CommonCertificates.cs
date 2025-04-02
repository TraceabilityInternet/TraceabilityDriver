
namespace TraceabilityDriver.Models.Mapping;

public class CommonCertificates
{
    /// <summary>
    /// The fishing authorization for the certificates.
    /// </summary>
    public CommonCertificate? FishingAuthorization { get; set; } = null;

    /// <summary>
    /// Merges common certificates into the current context.
    /// </summary>
    /// <param name="other">Contains the certificates to be merged into the existing set.</param>
    public void Merge(CommonCertificates other)
    {
        if (this.FishingAuthorization == null && other.FishingAuthorization != null)
        {
            this.FishingAuthorization = other.FishingAuthorization;
        }
        else if (this.FishingAuthorization != null && other.FishingAuthorization != null)
        {
            this.FishingAuthorization.Merge(other.FishingAuthorization);
        }
    }
}


