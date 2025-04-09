using System.Security.Cryptography;
using System.Text;

namespace TraceabilityDriver.Models.Mapping;

/// <summary>
/// The common event model that data from the database is mapped to
/// then the common event is used to create an event in the event store.
/// </summary>
public class CommonEvent : CommonBaseModel
{
    /// <summary>
    /// The id of the event. This is used for merging the events together.
    /// </summary>
    public string? EventId { get; set; } = null;

    /// <summary>
    /// The type of the event.
    /// </summary>
    public string? EventType { get; set; } = null;

    /// <summary>
    /// The time of the event.
    /// </summary>
    public DateTimeOffset? EventTime { get; set; } = null;

    /// <summary>
    /// The information provider of the event information.
    /// </summary>
    public CommonParty? InformationProvider { get; set; } = null;

    /// <summary>
    /// The owner of the product at the time of the event.
    /// </summary>
    public CommonParty? ProductOwner { get; set; } = null;

    /// <summary>
    /// The location of the event.
    /// </summary>
    public CommonLocation? Location { get; set; } = null;

    /// <summary>
    /// The certificates of the event.
    /// </summary>
    public CommonCertificates? Certificates { get; set; } = null;

    /// <summary>
    /// The product of the event.
    /// </summary>
    public List<CommonProduct>? Products { get; set; } = null;

    /// <summary>
    /// The catch information related to the event.
    /// </summary>
    public CommonCatchInformation? CatchInformation { get; set; } = null;

    public List<CommonSource>? SourceList { get; set; } = null;

    public List<CommonDestination>? DestinationList { get; set; } = null;

    public string? BroodStockSource { get; set; } = null;

    public string? HumanWelfarePolicy { get; set; } = null;

    public string? ProteinSource { get; set; } = null;

    public string? AquacultureMethod { get; set; } = null;

    /// <summary>
    /// Merges property values from the source onto the target
    /// only if that property value has no value and the source
    /// has a value.
    /// </summary>
    /// <param name="target">The object to copy values to.</param>
    /// <param name="source">The object to copy values from.</param>
    public void Merge(CommonEvent source)
    {
        if (this.EventTime == null && source.EventTime != null)
        {
            this.EventTime = source.EventTime;
        }

        // Brood stock Source
        if (this.BroodStockSource == null && source.BroodStockSource != null)
        {
            this.BroodStockSource = source.BroodStockSource;
        }

        // Human Welfare Policy
        if (this.HumanWelfarePolicy == null && source.HumanWelfarePolicy != null)
        {
            this.HumanWelfarePolicy = source.HumanWelfarePolicy;
        }

        // Protein Source
        if (this.ProteinSource == null && source.ProteinSource != null)
        {
            this.ProteinSource = source.ProteinSource;
        }

        // Aquaculture Method
        if (this.AquacultureMethod == null && source.AquacultureMethod != null)
        {
            this.AquacultureMethod = source.AquacultureMethod;
        }

        // Source List
        if (this.SourceList == null && source.SourceList != null)
        {
            this.SourceList = source.SourceList;
        }
        else if (this.SourceList != null && source.SourceList != null)
        {
            foreach (var sourceItem in source.SourceList)
            {
                var thisSource = this.SourceList.FirstOrDefault(s => s.Type == sourceItem.Type);
                if (thisSource == null)
                {
                    this.SourceList.Add(sourceItem);
                }
                else
                {
                    thisSource.Merge(sourceItem);
                }
            }
        }

        // Destination List
        if (this.DestinationList == null && source.DestinationList != null)
        {
            this.DestinationList = source.DestinationList;
        }
        else if (this.DestinationList != null && source.DestinationList != null)
        {
            foreach (var destinationItem in source.DestinationList)
            {
                var thisDestination = this.DestinationList.FirstOrDefault(d => d.Type == destinationItem.Type);
                if (thisDestination == null)
                {
                    this.DestinationList.Add(destinationItem);
                }
                else
                {
                    thisDestination.Merge(destinationItem);
                }
            }
        }

        // Product Owner
        if (this.ProductOwner == null && source.ProductOwner != null)
        {
            this.ProductOwner = source.ProductOwner;
        }
        else if (this.ProductOwner != null && source.ProductOwner != null)
        {
            this.ProductOwner.Merge(source.ProductOwner);
        }

        // Information Provider
        if (this.InformationProvider == null && source.InformationProvider != null)
        {
            this.InformationProvider = source.InformationProvider;
        }
        else if (this.InformationProvider != null && source.InformationProvider != null)
        {
            this.InformationProvider.Merge(source.InformationProvider);
        }

        // Location
        if (this.Location == null && source.Location != null)
        {
            this.Location = source.Location;
        }
        else if (this.Location != null && source.Location != null)
        {
            this.Location.Merge(source.Location);
        }

        // Certificates
        if (this.Certificates == null && source.Certificates != null)
        {
            this.Certificates = source.Certificates;
        }
        else if (this.Certificates != null && source.Certificates != null)
        {
            this.Certificates.Merge(source.Certificates);
        }

        // Catch Information
        if (this.CatchInformation == null && source.CatchInformation != null)
        {
            this.CatchInformation = source.CatchInformation;
        }
        else if (this.CatchInformation != null && source.CatchInformation != null)
        {
            this.CatchInformation.Merge(source.CatchInformation);
        }

        // Products
        if (this.Products == null && source.Products != null)
        {
            this.Products = source.Products;
        }
        else if (this.Products != null && source.Products != null)
        {
            foreach (var product in source.Products)
            {
                var thisProduct = this.Products.FirstOrDefault(p => p.ProductId == product.ProductId);

                if (thisProduct == null)
                {
                    this.Products.Add(product);
                }
                else
                {
                    thisProduct.Merge(product);
                }
            }
        }
    }

    /// <summary>
    /// Generates a SHA-256 hash from the event ID and returns in the format "urn:uuid:{hash}".
    /// </summary>
    /// <returns>The event ID.</returns>
    public Uri GetEpcisEventId()
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(this.EventId);

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{GDST_IDENTIFIERS_DOMAIN}:{this.EventId}"));

        return new Uri($"urn:uuid:{BitConverter.ToString(hash).Replace("-", "")}");
    }
}


