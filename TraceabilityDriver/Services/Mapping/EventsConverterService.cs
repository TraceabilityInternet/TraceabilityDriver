using OpenTraceability.GDST.Events;
using OpenTraceability.GDST.Events.KDEs;
using OpenTraceability.GDST.MasterData;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Events.KDEs;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Models.MasterData;
using OpenTraceability.MSC.Events;
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
        doc.EPCISVersion = EPCISVersion.V2;
        doc.CreationDate = DateTimeOffset.UtcNow;

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
                    case "gdstaggregationevent": ConvertTo_GDSTAggregationEvent(commonEvent, doc); break;
                    case "gdstdisaggregationevent": ConvertTo_GDSTDisaggregationEvent(commonEvent, doc); break;
                    case "gdstcomminglingevent": ConvertTo_GDSTComminglingEvent(commonEvent, doc); break;
                    case "gdstlandingevent": ConvertTo_GDSTLandingEvent(commonEvent, doc); break;
                    case "gdsttransshipmentevent": ConvertTo_GDSTTransshippmentEvent(commonEvent, doc); break;
                    case "gdstprocessingevent": ConvertTo_GDSTProcessingEvent(commonEvent, doc); break;
                    case "gdstfishingevent": ConvertTo_GDSTFishingEvent(commonEvent, doc); break;
                    case "gdstshippingevent": ConvertTo_GDSTShippingEvent(commonEvent, doc); break;
                    case "gdstreceiveevent": ConvertTo_GDSTReceiveEvent(commonEvent, doc); break;
                    case "gdstfarmharvestevent": ConvertTo_GDSTFarmHarvestEvent(commonEvent, doc); break;
                    case "gdstfarmharvestobjectevent": ConvertTo_GDSTFarmHarvestObjectEvent(commonEvent, doc); break;
                    case "gdsthatchingevent": ConvertTo_GDSTHatchingEvent(commonEvent, doc); break;
                    case "gdstfeedmillobjectevent": ConvertTo_GDSTFeedMillObjectEvent(commonEvent, doc); break;
                    case "gdstfeedmilltransformationevent": ConvertTo_GDSTFeedMillTransformationEvent(commonEvent, doc); break;
                    case "mscprocessingevent": ConvertTo_MSCProcessingevent(commonEvent, doc); break;
                    case "mscshippingevent": ConvertTo_MSCShippingEvent(commonEvent, doc); break;
                    case "mscreceiveevent": ConvertTo_MSCReceiveEvent(commonEvent, doc); break;
                    case "mscstorageevent": ConvertTo_MSCStorageEvent(commonEvent, doc); break;
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

    public void ConvertTo_GDSTTransshippmentEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTTransshipmentEvent epcisEvent = new GDSTTransshipmentEvent();

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

        // Certifications
        epcisEvent.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.CertificationList, commonEvent.Certificates);

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

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
    public void ConvertTo_GDSTLandingEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTLandingEvent epcisEvent = new GDSTLandingEvent();

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

        // Certifications
        epcisEvent.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.CertificationList, commonEvent.Certificates);

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

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

    public void ConvertTo_GDSTComminglingEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTComminglingEvent epcisEvent = new GDSTComminglingEvent();

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

    public void ConvertTo_GDSTAggregationEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTAggregationEvent epcisEvent = new GDSTAggregationEvent();

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

        // TODO: Implement read point

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

    public void ConvertTo_GDSTDisaggregationEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTDisaggregationEvent epcisEvent = new GDSTDisaggregationEvent();

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

        // TODO: Implement read point

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

    public void ConvertTo_MSCStorageEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        MSCStorageEvent epcisEvent = new MSCStorageEvent();

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

        // Certificates
        epcisEvent.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.CertificationList, commonEvent.Certificates);

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

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
            if (!string.IsNullOrEmpty(product.SSCC))
            {
                // Ensure the SSCC can be generated. This will throw an exception if it cannot be generated.
                EPC sscc = product.GenerateSSCC(product.SSCC);
            }
            else
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
            }
            if (product.ProductType == null)
            {
                error = "Product type is NULL.";
                return false;
            }
        }

        return true;
    }

    public void ConvertTo_MSCReceiveEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        MSCReceiveEvent epcisEvent = new MSCReceiveEvent();

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

        // Certificates
        epcisEvent.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.CertificationList, commonEvent.Certificates);

        // Products
        if (commonEvent.Products != null)
        {
            foreach (var product in commonEvent.Products)
            {
                SetProduct(epcisEvent, product, doc);
            }
        }

        // Source List
        epcisEvent.SourceList = new List<EventSource>();
        SetSourceList(epcisEvent.SourceList, commonEvent.Source);

        // Destination List
        epcisEvent.DestinationList = new List<EventDestination>();
        SetDestinationList(epcisEvent.DestinationList, commonEvent.Destination);

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

        // Transport
        epcisEvent.TransportType = commonEvent.TransportType;
        epcisEvent.TransportVehicleID = commonEvent.TransportVehicleID;
        epcisEvent.TransportNumber = commonEvent.TransportNumber;
        epcisEvent.TransportProviderID = commonEvent.TransportProviderID;

        doc.Events.Add(epcisEvent);
    }

    public void ConvertTo_MSCShippingEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        MSCShippingEvent epcisEvent = new MSCShippingEvent();

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

        // Certificates
        epcisEvent.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.CertificationList, commonEvent.Certificates);

        // Products
        if (commonEvent.Products != null)
        {
            foreach (var product in commonEvent.Products)
            {
                SetProduct(epcisEvent, product, doc);
            }
        }

        // Source List
        epcisEvent.SourceList = new List<EventSource>();
        SetSourceList(epcisEvent.SourceList, commonEvent.Source);

        // Destination List
        epcisEvent.DestinationList = new List<EventDestination>();
        SetDestinationList(epcisEvent.DestinationList, commonEvent.Destination);

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

        // Transport
        epcisEvent.TransportType = commonEvent.TransportType;
        epcisEvent.TransportVehicleID = commonEvent.TransportVehicleID;
        epcisEvent.TransportNumber = commonEvent.TransportNumber;
        epcisEvent.TransportProviderID = commonEvent.TransportProviderID;

        doc.Events.Add(epcisEvent);
    }

    public void ConvertTo_GDSTProcessingEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTProcessingEvent epcisEvent = new GDSTProcessingEvent();

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

        // Certificates
        epcisEvent.ILMD = new();
        epcisEvent.ILMD.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.ILMD.CertificationList, commonEvent.Certificates);

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

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

    public void ConvertTo_MSCProcessingevent(CommonEvent commonEvent, EPCISDocument doc)
    {
        MSCProcessingEvent epcisEvent = new MSCProcessingEvent();

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

        // Certificates
        epcisEvent.ILMD = new();
        epcisEvent.ILMD.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.ILMD.CertificationList, commonEvent.Certificates);

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

        // ILMD
        epcisEvent.ILMD.ProcessingType = commonEvent.ProcessingType;

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

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

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
    /// Converts the common event to a GDST Fishing Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTShippingEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTShippingEvent epcisEvent = new GDSTShippingEvent();

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

        // Certificates
        epcisEvent.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.CertificationList, commonEvent.Certificates);

        // Products
        if (commonEvent.Products != null)
        {
            foreach (var product in commonEvent.Products)
            {
                SetProduct(epcisEvent, product, doc);
            }
        }

        // Source List
        epcisEvent.SourceList = new List<EventSource>();
        SetSourceList(epcisEvent.SourceList, commonEvent.Source);

        // Destination List
        epcisEvent.DestinationList = new List<EventDestination>();
        SetDestinationList(epcisEvent.DestinationList, commonEvent.Destination);

        doc.Events.Add(epcisEvent);
    }

    /// <summary>
    /// Converts the common event to a GDST Fishing Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTReceiveEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTReceiveEvent epcisEvent = new GDSTReceiveEvent();

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

        // Certificates
        epcisEvent.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.CertificationList, commonEvent.Certificates);

        // Products
        if (commonEvent.Products != null)
        {
            foreach (var product in commonEvent.Products)
            {
                SetProduct(epcisEvent, product, doc);
            }
        }

        // Source List
        epcisEvent.SourceList = new List<EventSource>();
        SetSourceList(epcisEvent.SourceList, commonEvent.Source);

        // Destination List
        epcisEvent.DestinationList = new List<EventDestination>();
        SetDestinationList(epcisEvent.DestinationList, commonEvent.Destination);

        doc.Events.Add(epcisEvent);
    }

    public void ConvertTo_GDSTFarmHarvestObjectEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTFarmHarvestObjectEvent epcisEvent = new GDSTFarmHarvestObjectEvent();
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

        // Certificates
        epcisEvent.ILMD.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.ILMD.CertificationList, commonEvent.Certificates);

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

        // aquaculture method
        epcisEvent.ILMD.AquacultureMethod = commonEvent.AquacultureMethod;
        epcisEvent.ILMD.ProductionMethodForFishAndSeafoodCode = commonEvent.ProductionMethod;

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
    /// Converts the common event to a GDST Fishing Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTFarmHarvestEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTFarmHarvestEvent epcisEvent = new GDSTFarmHarvestEvent();
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

        // Certificates
        epcisEvent.ILMD.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.ILMD.CertificationList, commonEvent.Certificates);

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

        // aquaculture method
        epcisEvent.ILMD.AquacultureMethod = commonEvent.AquacultureMethod;
        epcisEvent.ILMD.ProductionMethodForFishAndSeafoodCode = commonEvent.ProductionMethod;

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
    /// Converts the common event to a GDST Fishing Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTHatchingEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTHatchingEvent epcisEvent = new GDSTHatchingEvent();
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

        // Certificates
        epcisEvent.ILMD.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.ILMD.CertificationList, commonEvent.Certificates);

        // brood stock source
        epcisEvent.ILMD.BroodstockSource = commonEvent.BroodStockSource;

        // human well fare policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

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
    /// Converts the common event to a GDST Fishing Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTFeedMillObjectEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTFeedmillObjectEvent epcisEvent = new GDSTFeedmillObjectEvent();
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

        // Certificates
        epcisEvent.ILMD.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.ILMD.CertificationList, commonEvent.Certificates);

        // humanWelfarePolicy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

        // protein source
        epcisEvent.ILMD.ProteinSource = commonEvent.ProteinSource;

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
    /// Converts the common event to a GDST Fishing Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTFeedMillTransformationEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTFeedmillTransformationEvent epcisEvent = new GDSTFeedmillTransformationEvent();
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

        // Certificates
        epcisEvent.ILMD.CertificationList = new CertificationList();
        SetEventCertificates(epcisEvent.ILMD.CertificationList, commonEvent.Certificates);

        // humanWelfarePolicy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

        // protein source
        epcisEvent.ILMD.ProteinSource = commonEvent.ProteinSource;

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

            if (certificates.ChainOfCustodyCertification != null)
            {
                certificationList.Certificates.Add(new OpenTraceability.Models.Common.Certificate()
                {
                    CertificateType = "urn:gdst:certType:harvestCoC",
                    Identification = certificates.ChainOfCustodyCertification.Identifier
                });
            }

            if (certificates.HumanPolicyCertificate != null)
            {
                certificationList.Certificates.Add(new OpenTraceability.Models.Common.Certificate()
                {
                    CertificateType = "urn:gdst:certType:humanPolicy",
                    Identification = certificates.HumanPolicyCertificate.Identifier
                });
            }

            if (certificates.HarvestCertification != null)
            {
                certificationList.Certificates.Add(new OpenTraceability.Models.Common.Certificate()
                {
                    CertificateType = "urn:gdst:certType:harvestCert",
                    Identification = certificates.HarvestCertification.Identifier
                });
            }
        }
    }

    public void SetSourceList(List<EventSource> eventSources, CommonSource? commonSource)
    {
        if (commonSource == null)
        {
            return;
        }

        if (commonSource.Party != null)
        {
            eventSources.Add(new EventSource()
            {
                Type = new Uri("urn:epcglobal:cbv:sdt:owning_party"),
                Value = commonSource.Party.GetPGLN().ToString()
            });
        }

        if(commonSource.Location != null)
        {
            eventSources.Add(new EventSource()
            {
                Type = new Uri("urn:epcglobal:cbv:sdt:location"),
                Value = commonSource.Location.GetGLN().ToString()
            });
        }
    }

    public void SetDestinationList(List<EventDestination> eventDestinations, CommonDestination? commonDestination)
    {
        if (commonDestination == null)
        {
            return;
        }

        if (commonDestination.Party != null)
        {
            eventDestinations.Add(new EventDestination()
            {
                Type = new Uri("urn:epcglobal:cbv:sdt:owning_party"),
                Value = commonDestination.Party.GetPGLN().ToString()
            });
        }

        if (commonDestination.Location != null)
        {
            eventDestinations.Add(new EventDestination()
            {
                Type = new Uri("urn:epcglobal:cbv:sdt:location"),
                Value = commonDestination.Location.GetGLN().ToString()
            });
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

