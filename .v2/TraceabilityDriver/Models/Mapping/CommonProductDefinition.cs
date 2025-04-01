
using OpenTraceability.Models.Identifiers;

namespace TraceabilityDriver.Models.Mapping;

public class CommonProductDefinition : CommonBaseModel
{
    /// <summary>
    /// A unique identifier for this product definition.
    /// </summary>
    public string? ProductDefinitionId { get; set; } = null;

    /// <summary>
    /// The owner ID of the product definition.
    /// </summary>
    public string? OwnerId { get; set; } = null;

    /// <summary>
    /// The short description of the product definition.
    /// </summary>
    public string? ShortDescription { get; set; } = null;

    /// <summary>
    /// The product form of the product.
    /// </summary>  
    public string? ProductForm { get; set; } = null;

    /// <summary>
    /// The scientific name of the species of the product.
    /// </summary>
    public string? ScientificName { get; set; } = null;

    /// <summary>
    /// Combines the current product definition with another product instance by merging the properties. Only properties that are null are merged.
    /// </summary>
    /// <param name="other">The product definition instance to merge with the current one.</param>
    public void Merge(CommonProductDefinition other)
    {
        if (this.OwnerId == null && other.OwnerId != null)
        {
            this.OwnerId = other.OwnerId;
        }
        if (this.ShortDescription == null && other.ShortDescription != null)
        {
            this.ShortDescription = other.ShortDescription;
        }
        if (this.ProductForm == null && other.ProductForm != null)
        {
            this.ProductForm = other.ProductForm;
        }
        if (this.ScientificName == null && other.ScientificName != null)
        {
            this.ScientificName = other.ScientificName;
        }
    }

    /// <summary>
    /// This method will attempt to generate a GTIN for the product definition.
    /// </summary>
    /// <returns></returns>
    public GTIN GetGTIN()
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(this.ProductDefinitionId);

        // If the product definition ID is already a GTIN, then just parse that and return it.
        if (GTIN.IsGTIN(this.ProductDefinitionId))
        {
            return new GTIN(this.ProductDefinitionId);
        }
        else
        {
            // Ensure we have an owner ID set.
            ArgumentNullException.ThrowIfNullOrWhiteSpace(this.OwnerId);

            // Generate the GTIN.
            string gtin = $"urn:gdst:{GDST_IDENTIFIERS_DOMAIN}:product:class:{NormalizeString(this.OwnerId)}.{NormalizeString(this.ProductDefinitionId)}";
            return new GTIN(gtin);
        }
    }
}


