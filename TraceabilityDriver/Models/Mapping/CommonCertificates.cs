
namespace TraceabilityDriver.Models.Mapping;

public class CommonCertificates
{
    /// <summary>
    /// The fishing authorization for the certificates.
    /// </summary>
    public CommonCertificate? FishingAuthorization { get; set; } = null;

    /// <summary>
    /// The harvest certificate for the certificates.
    /// </summary>
    public CommonCertificate? HarvestCertification { get; set; } = null;

    /// <summary>
    /// The chain of custody certification for the certificates.
    /// </summary>
    public CommonCertificate? ChainOfCustodyCertification { get; set; } = null;

    /// <summary>
    /// The human policy certificate for the certificates.
    /// </summary>
    public CommonCertificate? HumanPolicyCertificate { get; set; } = null;

    /// <summary>
    /// The transhipment authority for the certificates.
    /// </summary>
    public CommonCertificate? TransshipmentAuthority { get; set; } = null;

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

        if (this.HarvestCertification == null && other.HarvestCertification != null)
        {
            this.HarvestCertification = other.HarvestCertification;
        }
        else if (this.HarvestCertification != null && other.HarvestCertification != null)
        {
            this.HarvestCertification.Merge(other.HarvestCertification);
        }

        if (this.ChainOfCustodyCertification == null && other.ChainOfCustodyCertification != null)
        {
            this.ChainOfCustodyCertification = other.ChainOfCustodyCertification;
        }
        else if (this.ChainOfCustodyCertification != null && other.ChainOfCustodyCertification != null)
        {
            this.ChainOfCustodyCertification.Merge(other.ChainOfCustodyCertification);
        }

        if (this.HumanPolicyCertificate == null && other.HumanPolicyCertificate != null)
        {
            this.HumanPolicyCertificate = other.HumanPolicyCertificate;
        }
        else if (this.HumanPolicyCertificate != null && other.HumanPolicyCertificate != null)
        {
            this.HumanPolicyCertificate.Merge(other.HumanPolicyCertificate);
        }

        if (this.TransshipmentAuthority == null && other.TransshipmentAuthority != null)
        {
            this.TransshipmentAuthority = other.TransshipmentAuthority;
        }
        else if (this.TransshipmentAuthority != null && other.TransshipmentAuthority != null)
        {
            this.TransshipmentAuthority.Merge(other.TransshipmentAuthority);
        }
    }
}


