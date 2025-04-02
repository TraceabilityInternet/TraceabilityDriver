
namespace TraceabilityDriver.Models.Mapping;

public class CommonCertificate
{
    /// <summary>
    /// The identifier for the certificate.
    /// </summary>
    public string? Identifier { get; set; } = null;

    /// <summary>
    /// Merge the certificate onto this one. Properties are only merged if they are null.
    /// </summary>
    /// <param name="other">The other certificate.</param>
    public void Merge(CommonCertificate other)
    {
        if (this.Identifier == null && other.Identifier != null)
        {
            this.Identifier = other.Identifier;
        }
    }
}


