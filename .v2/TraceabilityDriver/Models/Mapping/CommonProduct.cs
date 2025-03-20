
using OpenTraceability.Models.Identifiers;

namespace TraceabilityDriver.Models.Mapping;

public class CommonProduct
{
    /// <summary>
    /// A unique identifier for this product to be used for merging.
    /// </summary>
    public string? ProductId { get; set; } = null;

    /// <summary>
    /// The type of the product.
    /// </summary>
    public OpenTraceability.Models.Events.EventProductType? ProductType { get; set; } = OpenTraceability.Models.Events.EventProductType.Reference;

    /// <summary>
    /// The lot number of the product.
    /// </summary>
    public string? LotNumber { get; set; } = null;

    /// <summary>
    /// The serial number of the product.
    /// </summary>
    public string? SerialNumber { get; set; } = null;

    /// <summary>
    /// The quantity of the product.
    /// </summary>
    public decimal? Quantity { get; set; } = 0;

    /// <summary>
    /// The unit of measure for the product.
    /// </summary>
    public string? UoM { get; set; } = null;

    /// <summary>
    /// The product definition.
    /// </summary>
    public CommonProductDefinition? ProductDefinition { get; set; } = null;

    /// <summary>
    /// Either parses the EPC from the ProductId or tries to generate one.
    /// </summary>
    /// <returns></returns>
    public EPC GetEPC()
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(this.ProductId);

        // If the product definition ID is already a GTIN, then just parse that and return it.
        if (EPC.TryParse(this.ProductId, out EPC epc, out string err))
        {
            return epc;
        }
        else
        {
            ArgumentNullException.ThrowIfNull(this.ProductDefinition);

            GTIN gtin = this.ProductDefinition.GetGTIN();

            if (!string.IsNullOrWhiteSpace(this.LotNumber))
            {
                epc = new EPC(EPCType.Class, gtin, this.LotNumber);
            }
            else if (!string.IsNullOrWhiteSpace(this.SerialNumber))
            {
                epc = new EPC(EPCType.Instance, gtin, this.SerialNumber);
            }
            else
            {
                throw new Exception("Either the lot number or the serial number must be set to generate an EPC.");
            }

            return epc;
        }
    }

    /// <summary>
    /// Combines the current product with another product instance by merging the properties. Only properties that are null are merged.
    /// </summary>
    /// <param name="other">The product instance to merge with the current one.</param>
    public void Merge(CommonProduct other)
    {
        if (this.LotNumber == null && other.LotNumber != null)
        {
            this.LotNumber = other.LotNumber;
        }
        if (this.SerialNumber == null && other.SerialNumber != null)
        {
            this.SerialNumber = other.SerialNumber;
        }
        if (this.Quantity == 0 && other.Quantity != 0)
        {
            this.Quantity = other.Quantity;
        }
        if (this.UoM == null && other.UoM != null)
        {
            this.UoM = other.UoM;
        }
        if (this.ProductDefinition == null && other.ProductDefinition != null)
        {
            this.ProductDefinition = other.ProductDefinition;
        }
        else if (this.ProductDefinition != null && other.ProductDefinition != null)
        {
            this.ProductDefinition.Merge(other.ProductDefinition);
        }
    }
}


