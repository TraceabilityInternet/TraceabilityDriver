
namespace TraceabilityDriver.Models.Mapping;

public class CommonCertificate
{
    /// <summary>
    /// The identifier for the certificate.
    /// </summary>
    public string? Identifier { get; set; } = null;

    /// <summary>
    /// The agency that issued the certificate.
    /// </summary>
    public string? Agency { get; set; } = null;

    /// <summary>
    /// The standard the certificate was issued against.
    /// </summary>
    public string? Standard { get; set; } = null;

    /// <summary>
    /// The value of the certificate.
    /// </summary>
    public string? Value { get; set; } = null;

    /// <summary>
    /// Returns TRUE when the certificate carries no data at all. The table mapping service
    /// instantiates intermediate objects even when every mapped column is NULL, so an empty
    /// certificate must be detectable and skipped during conversion.
    /// </summary>
    public bool IsEmpty()
    {
        return this.Identifier == null && this.Agency == null && this.Standard == null && this.Value == null;
    }

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

        if (this.Agency == null && other.Agency != null)
        {
            this.Agency = other.Agency;
        }

        if (this.Standard == null && other.Standard != null)
        {
            this.Standard = other.Standard;
        }

        if (this.Value == null && other.Value != null)
        {
            this.Value = other.Value;
        }
    }
}


