using OpenTraceability.GDST.Events;
using OpenTraceability.GDST.Events.KDEs;
using OpenTraceability.GDST.MasterData;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Events.KDEs;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Models.MasterData;
using TraceabilityDriver.Models;
using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Services;

/// <summary>
/// The service for converting common events to EPCIS events.
/// </summary>
public class EventsConverterService : IEventsConverterService
{
    private readonly ILogger<EventsConverterService> _logger;

    public EventsConverterService(ILogger<EventsConverterService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Converts the common events to EPCIS events.
    /// </summary>
    public Task<EPCISDocument> ConvertEventsAsync(List<CommonEvent> events)
    {
        EPCISDocument doc = new EPCISDocument();

        foreach (var commonEvent in events)
        {
            try
            {
                if (!IsEventValid(commonEvent, out string error))
                {
                    _logger.LogError("Event is not valid for conversion: {EventId} with {Error}", commonEvent.EventId, error);
                    continue;
                }

                switch (commonEvent.EventType?.Trim().ToLower())
                {
                    case "gdstfishingevent": ConvertTo_GDSTFishingEvent(commonEvent, doc); break;
                    default:
                        _logger.LogError("Event type not supported: {EventType}", commonEvent.EventType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting event: {EventId}", commonEvent.EventId);
            }
        }

        return Task.FromResult(doc);
    }

    /// <summary>
    /// Determines if the event is valid for converting to an EPCIS event.
    /// </summary>
    /// <param name="commonEvent">The event to validate.</param>
    /// <returns>TRUE if the event is valid, otherwise FALSE.</returns>
    public bool IsEventValid(CommonEvent commonEvent, out string error)
    {
        error = string.Empty;

        if (commonEvent.Products == null)
        {
            error = "Products is NULL.";
            return false;
        }

        if (!commonEvent.Products.Any())
        {
            error = "No products found on the event.";
            return false;
        }

        foreach (var product in commonEvent.Products)
        {
            if (product.ProductDefinition == null)
            {
                error = "Product definition is NULL.";
                return false;
            }
            if (product.ProductDefinition.GetGTIN() == null)
            {
                error = "GTIN is NULL.";
                return false;
            }
            if (product.ProductType == null)
            {
                error = "Product type is NULL.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Converts the common event to a GDST Fishing Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTFishingEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTFishingEvent epcisEvent = new GDSTFishingEvent();
        epcisEvent.ILMD = new GDSTILMD();

        // Event ID
        epcisEvent.EventID = commonEvent.GetEpcisEventId();

        // Event Time
        epcisEvent.EventTime = commonEvent.EventTime;
        epcisEvent.EventTimeZoneOffset = TimeSpan.FromMinutes(0);

        // Information Provider
        epcisEvent.InformationProvider = SetPartyMasterData(commonEvent.InformationProvider, doc);

        // Product Owner
        epcisEvent.ProductOwner = SetPartyMasterData(commonEvent.ProductOwner, doc);

        // Location
        SetEventLocation(epcisEvent, commonEvent.Location, doc);

        // Catch Area
        if (commonEvent.CatchInformation != null)
        {
            epcisEvent.ILMD.VesselCatchInformationList = new VesselCatchInformationList();
            epcisEvent.ILMD.VesselCatchInformationList.Vessels.Add(new VesselCatchInformation()
            {
                CatchArea = commonEvent.CatchInformation.CatchArea,
                GearType = commonEvent.CatchInformation.GearType,
                GPSAvailability = commonEvent.CatchInformation.GPSAvailable
            });
        }

        // Certificates
        epcisEvent.ILMD.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.ILMD.CertificationList, commonEvent.Certificates);

        // Products
        if (commonEvent.Products != null)
        {
            foreach (var product in commonEvent.Products)
            {
                SetProduct(epcisEvent, product, doc);
            }
        }

        doc.Events.Add(epcisEvent);
    }

    /// <summary>
    /// Converts the common event to a GDST Landing Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void SetEventCertificates(CertificationList certificationList, CommonCertificates? certificates)
    {
        if (certificates != null)
        {
            certificationList.Certificates = certificationList.Certificates != null ? certificationList.Certificates : new List<OpenTraceability.Models.Common.Certificate>();
            if (certificates.FishingAuthorization != null)
            {
                certificationList.Certificates.Add(new OpenTraceability.Models.Common.Certificate()
                {
                    CertificateType = "urn:gdst:certType:fishingAuth",
                    Identification = certificates.FishingAuthorization.Identifier
                });
            }
        }
    }

    /// <summary>
    /// Adds the location to the master data if it is not added already.
    /// </summary>
    public PGLN? SetPartyMasterData(CommonParty? party, EPCISDocument doc)
    {
        if (party == null)
        {
            return null;
        }
        else
        {
            TradingParty tradingParty = new TradingParty();
            tradingParty.PGLN = party.GetPGLN();
            tradingParty.Name = new List<OpenTraceability.Models.Common.LanguageString>();
            tradingParty.Name.Add(new OpenTraceability.Models.Common.LanguageString() { Language = "en-US", Value = party.Name });

            if (doc.MasterData.All(x => x.ID != tradingParty.PGLN.ToString()))
            {
                doc.MasterData.Add(tradingParty);
            }

            return party.GetPGLN();
        }
    }

    /// <summary>
    /// Adds the location to the master data if it is not added already.
    /// </summary>
    public void SetEventLocation(IEvent evt, CommonLocation? location, EPCISDocument doc)
    {
        if (location != null)
        {
            if (evt.Location == null)
            {
                evt.Location = new EventLocation();
            }

            // Set the GLN on the event.
            evt.Location.GLN = location.GetGLN();

            // Create the master data object.
            GDSTLocation loc = new GDSTLocation();
            loc.GLN = evt.Location.GLN;
            loc.Name = new List<OpenTraceability.Models.Common.LanguageString>();
            loc.Name.Add(new OpenTraceability.Models.Common.LanguageString() { Language = "en-US", Value = location.Name });
            loc.Address = new OpenTraceability.Models.MasterData.Address();
            if (location.OwnerId != null)
            {
                loc.OwningParty = location.GeneratePGLN(location.OwnerId);
            }
            if (location.Country != null)
            {
                loc.Address.Country = location.Country;
            }
            loc.VesselID = location.LocationId;

            // Add it to the master data if it does not exist.
            if (doc.MasterData.All(x => x.ID != loc.GLN.ToString()))
            {
                doc.MasterData.Add(loc);
            }
        }
    }

    /// <summary>
    /// Adds the product to the master data if it is not added already.
    /// </summary>
    /// <param name="productDef">The product definition to add.</param>
    /// <param name="doc">The EPCIS document to add the product to.</param>
    /// <returns>The GTIN of the product.</returns>
    public GTIN? SetProductMasterData(CommonProductDefinition productDef, EPCISDocument doc)
    {
        OpenTraceability.Models.MasterData.Tradeitem tradeItem = new Tradeitem();

        tradeItem.GTIN = productDef.GetGTIN();
        tradeItem.ShortDescription = new List<OpenTraceability.Models.Common.LanguageString>();
        tradeItem.ShortDescription.Add(new OpenTraceability.Models.Common.LanguageString() { Language = "en-US", Value = productDef.ShortDescription });
        tradeItem.TradeItemConditionCode = productDef.ProductForm;
        tradeItem.FisherySpeciesScientificName = new List<string>();

        if (!string.IsNullOrWhiteSpace(productDef.ScientificName))
        {
            tradeItem.FisherySpeciesScientificName.Add(productDef.ScientificName);
        }
        
        if (productDef.OwnerId != null)
        {
            tradeItem.OwningParty = productDef.GeneratePGLN(productDef.OwnerId);
        }

        if (doc.MasterData.All(x => x.ID != tradeItem.GTIN.ToString()))
        {
            doc.MasterData.Add(tradeItem);
        }

        return productDef.GetGTIN();
    }

    /// <summary>
    /// Sets a product using the provided common product and EPCIS document.
    /// </summary>
    /// <param name="product">Represents the product to be set, containing relevant details.</param>
    /// <param name="doc">Contains the EPCIS document that provides context for the product.</param>
    public void SetProduct(IEvent evt, CommonProduct product, EPCISDocument doc)
    {
        EPC epc = product.GetEPC();

        if (product.ProductType == null)
        {
            throw new NullReferenceException("Product type is required.");
        }

        var eventProduct = new EventProduct(epc)
        {
            Type = product.ProductType!.Value,
        };

        if (product.Quantity != null && product.UoM != null)
        {
            eventProduct.Quantity = new OpenTraceability.Utility.Measurement(product.Quantity.Value, product.UoM);
        }

        evt.AddProduct(eventProduct);

        if (product.ProductDefinition != null)
        {
            SetProductMasterData(product.ProductDefinition, doc);
        }
    }
}

