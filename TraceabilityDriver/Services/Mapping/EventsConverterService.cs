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
/// <remarks>
/// GDST events are converted using the generic GDST 2.0 event set. The semantic meaning of an
/// event (fishing, landing, processing, etc.) is no longer carried by the event class; it is
/// derived downstream from the product and location classifications on the master data.
/// </remarks>
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
                    _logger.LogError("Event is not valid for conversion: {EventKey} with {Error}", commonEvent.EventKey, error);
                    continue;
                }

                switch (commonEvent.EventType?.Trim().ToLower())
                {
                    case "aggregationevent": ConvertTo_GDSTAggregationEvent(commonEvent, doc); break;
                    case "disaggregationevent": ConvertTo_GDSTDisaggregationEvent(commonEvent, doc); break;
                    case "commissioningevent": ConvertTo_GDSTCommissioningEvent(commonEvent, doc); break;
                    case "decommissioningevent": ConvertTo_GDSTDecommissioningEvent(commonEvent, doc); break;
                    case "shippingevent": ConvertTo_GDSTShippingEvent(commonEvent, doc); break;
                    case "receivingevent": ConvertTo_GDSTReceivingEvent(commonEvent, doc); break;
                    case "transformationevent": ConvertTo_GDSTTransformationEvent(commonEvent, doc); break;
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
                _logger.LogError(ex, "Error converting event: {EventKey}", commonEvent.EventKey);
            }
        }

        return Task.FromResult(doc);
    }

    /// <summary>
    /// Converts the common event to a GDST Commission Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTCommissioningEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTCommissionEvent epcisEvent = new GDSTCommissionEvent();
        epcisEvent.ILMD = new GDSTILMD();

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

        // Event Time
        epcisEvent.EventTime = commonEvent.EventTime;
        epcisEvent.EventTimeZoneOffset = TimeSpan.FromMinutes(0);

        // Information Provider
        epcisEvent.InformationProvider = SetPartyMasterData(commonEvent.InformationProvider, doc);

        // Product Owner
        epcisEvent.ProductOwner = SetPartyMasterData(commonEvent.ProductOwner, doc);

        // Location
        SetEventLocation(epcisEvent, commonEvent.Location, doc);

        // Read Point / Disposition
        SetReadPointAndDisposition(epcisEvent, commonEvent);

        // ILMD
        SetILMD(epcisEvent.ILMD, commonEvent);

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
    /// Converts the common event to a GDST Decommission Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTDecommissioningEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTDecommissionEvent epcisEvent = new GDSTDecommissionEvent();

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

        // Event Time
        epcisEvent.EventTime = commonEvent.EventTime;
        epcisEvent.EventTimeZoneOffset = TimeSpan.FromMinutes(0);

        // Information Provider
        epcisEvent.InformationProvider = SetPartyMasterData(commonEvent.InformationProvider, doc);

        // Product Owner
        epcisEvent.ProductOwner = SetPartyMasterData(commonEvent.ProductOwner, doc);

        // Location
        SetEventLocation(epcisEvent, commonEvent.Location, doc);

        // Read Point / Disposition
        SetReadPointAndDisposition(epcisEvent, commonEvent);

        // Certificates
        SetEventCertificates(epcisEvent, commonEvent.Certificates);

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
    /// Converts the common event to a GDST Aggregation Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTAggregationEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTAggregationEvent epcisEvent = new GDSTAggregationEvent();

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

        // Event Time
        epcisEvent.EventTime = commonEvent.EventTime;
        epcisEvent.EventTimeZoneOffset = TimeSpan.FromMinutes(0);

        // Information Provider
        epcisEvent.InformationProvider = SetPartyMasterData(commonEvent.InformationProvider, doc);

        // Product Owner
        epcisEvent.ProductOwner = SetPartyMasterData(commonEvent.ProductOwner, doc);

        // Location
        SetEventLocation(epcisEvent, commonEvent.Location, doc);

        // Read Point / Disposition
        SetReadPointAndDisposition(epcisEvent, commonEvent);

        // Certificates
        SetEventCertificates(epcisEvent, commonEvent.Certificates);

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
    /// Converts the common event to a GDST Disaggregation Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTDisaggregationEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTDisaggregationEvent epcisEvent = new GDSTDisaggregationEvent();

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

        // Event Time
        epcisEvent.EventTime = commonEvent.EventTime;
        epcisEvent.EventTimeZoneOffset = TimeSpan.FromMinutes(0);

        // Information Provider
        epcisEvent.InformationProvider = SetPartyMasterData(commonEvent.InformationProvider, doc);

        // Product Owner
        epcisEvent.ProductOwner = SetPartyMasterData(commonEvent.ProductOwner, doc);

        // Location
        SetEventLocation(epcisEvent, commonEvent.Location, doc);

        // Read Point / Disposition
        SetReadPointAndDisposition(epcisEvent, commonEvent);

        // Certificates
        SetEventCertificates(epcisEvent, commonEvent.Certificates);

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
    /// Converts the common event to a GDST Shipping Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTShippingEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTShippingEvent epcisEvent = new GDSTShippingEvent();

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

        // Event Time
        epcisEvent.EventTime = commonEvent.EventTime;
        epcisEvent.EventTimeZoneOffset = TimeSpan.FromMinutes(0);

        // Information Provider
        epcisEvent.InformationProvider = SetPartyMasterData(commonEvent.InformationProvider, doc);

        // Location
        SetEventLocation(epcisEvent, commonEvent.Location, doc);

        // Read Point / Disposition
        SetReadPointAndDisposition(epcisEvent, commonEvent);

        // Certificates
        SetEventCertificates(epcisEvent, commonEvent.Certificates);

        // Unloading Port
        epcisEvent.UnloadingPort = commonEvent.UnloadingPort;

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
    /// Converts the common event to a GDST Receiving Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTReceivingEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTReceivingEvent epcisEvent = new GDSTReceivingEvent();

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

        // Event Time
        epcisEvent.EventTime = commonEvent.EventTime;
        epcisEvent.EventTimeZoneOffset = TimeSpan.FromMinutes(0);

        // Information Provider
        epcisEvent.InformationProvider = SetPartyMasterData(commonEvent.InformationProvider, doc);

        // Location
        SetEventLocation(epcisEvent, commonEvent.Location, doc);

        // Read Point / Disposition
        SetReadPointAndDisposition(epcisEvent, commonEvent);

        // Certificates
        SetEventCertificates(epcisEvent, commonEvent.Certificates);

        // Human Welfare Policy
        epcisEvent.HumanWelfarePolicy = commonEvent.HumanWelfarePolicy;

        // Unloading Port
        epcisEvent.UnloadingPort = commonEvent.UnloadingPort;

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
    /// Converts the common event to a GDST Transformation Event.
    /// </summary>
    /// <param name="commonEvent">The common event to convert.</param>
    /// <param name="doc">The EPCIS document to add the event to.</param>
    public void ConvertTo_GDSTTransformationEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        GDSTTransformationEvent epcisEvent = new GDSTTransformationEvent();
        epcisEvent.ILMD = new GDSTILMD();

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

        // Event Time
        epcisEvent.EventTime = commonEvent.EventTime;
        epcisEvent.EventTimeZoneOffset = TimeSpan.FromMinutes(0);

        // Information Provider
        epcisEvent.InformationProvider = SetPartyMasterData(commonEvent.InformationProvider, doc);

        // Product Owner
        epcisEvent.ProductOwner = SetPartyMasterData(commonEvent.ProductOwner, doc);

        // Location
        SetEventLocation(epcisEvent, commonEvent.Location, doc);

        // Read Point / Disposition
        SetReadPointAndDisposition(epcisEvent, commonEvent);

        // ILMD
        SetILMD(epcisEvent.ILMD, commonEvent);

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

    public void ConvertTo_MSCStorageEvent(CommonEvent commonEvent, EPCISDocument doc)
    {
        MSCStorageEvent epcisEvent = new MSCStorageEvent();

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

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

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

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

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

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

    public void ConvertTo_MSCProcessingevent(CommonEvent commonEvent, EPCISDocument doc)
    {
        MSCProcessingEvent epcisEvent = new MSCProcessingEvent();

        // Event Key
        epcisEvent.EventID = commonEvent.GetEventKey();

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
    /// Populates the GDST ILMD from the common event, setting only the KDEs the source data supplies.
    /// </summary>
    /// <remarks>
    /// The generic GDST 2.0 events no longer carry per-profile ILMD shapes, so a single method maps
    /// every ILMD KDE the common model supports (catch information, certificates, aquaculture and
    /// feed KDEs) and leaves the rest null.
    /// </remarks>
    /// <param name="ilmd">The ILMD to populate.</param>
    /// <param name="commonEvent">The common event to read the KDEs from.</param>
    public void SetILMD(GDSTILMD ilmd, CommonEvent commonEvent)
    {
        // Catch Information
        if (commonEvent.CatchInformation != null)
        {
            ilmd.VesselCatchInformationList = new VesselCatchInformationList();
            ilmd.VesselCatchInformationList.Vessels.Add(new VesselCatchInformation()
            {
                CatchArea = commonEvent.CatchInformation.CatchArea,
                GearType = commonEvent.CatchInformation.GearType,
                GPSAvailability = commonEvent.CatchInformation.GPSAvailable,
                EconomicZone = commonEvent.CatchInformation.EconomicZone,
                FIP = commonEvent.CatchInformation.FisheryImprovementProject,
                RFMO = commonEvent.CatchInformation.RfmoArea,
                SatelliteTrackingAuthority = commonEvent.CatchInformation.SatelliteTrackingAuthority,
                SubNationalPermitArea = commonEvent.CatchInformation.SubnationalPermitArea,
                VesselTripDate = commonEvent.CatchInformation.VesselTripDate
            });
        }

        // Certificates. The list is only assigned when at least one certificate exists so that
        // events without certificates serialize without an empty certification list.
        CertificationList certificationList = new CertificationList();
        SetEventCertificates(certificationList, commonEvent.Certificates);
        if (certificationList.Certificates.Any())
        {
            ilmd.CertificationList = certificationList;
        }

        // Country of Origin
        if (commonEvent.CountryOfOrigin != null)
        {
            ilmd.CountryOfOrigin.Add(OpenTraceability.Utility.Countries.Parse(commonEvent.CountryOfOrigin));
        }

        // Brood Stock Source
        if (commonEvent.BroodStockSource != null)
        {
            ilmd.BroodstockSource = commonEvent.BroodStockSource;
        }

        // Aquaculture Method
        if (commonEvent.AquacultureMethod != null)
        {
            ilmd.AquacultureMethod = commonEvent.AquacultureMethod;
        }

        // Protein Source
        if (commonEvent.ProteinSource != null)
        {
            ilmd.ProteinSource = commonEvent.ProteinSource;
        }

        // Production Method
        if (commonEvent.ProductionMethod != null)
        {
            ilmd.ProductionMethodForFishAndSeafoodCode = commonEvent.ProductionMethod;
        }
    }

    /// <summary>
    /// Builds the certification list from the common certificates and assigns it to the event
    /// only when at least one certificate exists, so events without certificates serialize
    /// without an empty certification list.
    /// </summary>
    /// <param name="epcisEvent">The event to assign the certification list to.</param>
    /// <param name="certificates">The common event certificates to convert.</param>
    public void SetEventCertificates(EventBase epcisEvent, CommonCertificates? certificates)
    {
        CertificationList certificationList = new CertificationList();
        SetEventCertificates(certificationList, certificates);
        if (certificationList.Certificates.Any())
        {
            epcisEvent.CertificationList = certificationList;
        }
    }

    /// <summary>
    /// Converts the common event certificates into the certification list. Certificate slots whose
    /// fields are all null are skipped because the table mapping service instantiates intermediate
    /// objects even when every mapped column is NULL.
    /// </summary>
    /// <param name="certificationList">The certification list to add the certificates to.</param>
    /// <param name="certificates">The common event certificates to convert.</param>
    public void SetEventCertificates(CertificationList certificationList, CommonCertificates? certificates)
    {
        if (certificates != null)
        {
            certificationList.Certificates = certificationList.Certificates != null ? certificationList.Certificates : new List<OpenTraceability.Models.Common.Certificate>();

            AddCertificate(certificationList, "urn:gdst:certType:fishingAuth", certificates.FishingAuthorization);
            AddCertificate(certificationList, "urn:gdst:certType:harvestCoC", certificates.ChainOfCustodyCertification);
            AddCertificate(certificationList, "urn:gdst:certType:humanPolicy", certificates.HumanPolicyCertificate);
            AddCertificate(certificationList, "urn:gdst:certType:harvestCert", certificates.HarvestCertification);
            AddCertificate(certificationList, "urn:gdst:certType:transshipmentAuth", certificates.TransshipmentAuthority);
            AddCertificate(certificationList, "urn:gdst:certType:processorLicense", certificates.ProcessorLicense);
            AddCertificate(certificationList, "urn:gdst:certType:landingAuth", certificates.LandingAuthorization);
            AddCertificate(certificationList, "urn:gdst:certType:legalAuth", certificates.LegalAuthorization);
        }
    }

    /// <summary>
    /// Adds a single certificate to the certification list when it carries any data.
    /// </summary>
    /// <param name="certificationList">The certification list to add the certificate to.</param>
    /// <param name="certificateType">The GDST certificate type URN.</param>
    /// <param name="certificate">The common certificate to convert, or null when the source data has none.</param>
    private void AddCertificate(CertificationList certificationList, string certificateType, CommonCertificate? certificate)
    {
        if (certificate == null || certificate.IsEmpty())
        {
            return;
        }

        certificationList.Certificates.Add(new OpenTraceability.Models.Common.Certificate()
        {
            CertificateType = certificateType,
            Agency = certificate.Agency,
            Standard = certificate.Standard,
            Value = certificate.Value,
            Identification = certificate.Identifier
        });
    }

    /// <summary>
    /// Sets the read point and disposition on the event when the source data supplies them. The
    /// GDST event constructors hardcode dispositions only for commissioning, aggregation, and
    /// packing events, so the remaining event types carry the disposition through the mapping.
    /// </summary>
    /// <param name="epcisEvent">The event to set the values on.</param>
    /// <param name="commonEvent">The common event to read the values from.</param>
    public void SetReadPointAndDisposition(IEvent epcisEvent, CommonEvent commonEvent)
    {
        if (commonEvent.ReadPoint != null)
        {
            epcisEvent.ReadPoint = new EventReadPoint() { ID = new Uri(commonEvent.ReadPoint) };
        }

        if (commonEvent.Disposition != null)
        {
            epcisEvent.Disposition = new Uri(commonEvent.Disposition, UriKind.RelativeOrAbsolute);
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

        if (commonSource.Location != null)
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
    /// Adds the party to the master data if it is not added already.
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
            tradingParty.InformationProvider = tradingParty.PGLN;
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
                loc.InformationProvider = loc.OwningParty;
            }
            if (location.Country != null)
            {
                loc.Address.Country = location.Country;
            }

            // Address
            if (location.Address1 != null)
            {
                loc.Address.Address1 = new List<OpenTraceability.Models.Common.LanguageString>() { new OpenTraceability.Models.Common.LanguageString() { Language = "en-US", Value = location.Address1 } };
            }
            if (location.Address2 != null)
            {
                loc.Address.Address2 = new List<OpenTraceability.Models.Common.LanguageString>() { new OpenTraceability.Models.Common.LanguageString() { Language = "en-US", Value = location.Address2 } };
            }
            if (location.City != null)
            {
                loc.Address.City = new List<OpenTraceability.Models.Common.LanguageString>() { new OpenTraceability.Models.Common.LanguageString() { Language = "en-US", Value = location.City } };
            }
            if (location.State != null)
            {
                loc.Address.State = new List<OpenTraceability.Models.Common.LanguageString>() { new OpenTraceability.Models.Common.LanguageString() { Language = "en-US", Value = location.State } };
            }
            if (location.PostalCode != null)
            {
                loc.Address.PostalCode = location.PostalCode;
            }

            // Geo Location / Geo Fence
            if (location.GeoLocation != null)
            {
                loc.GeoLocation = location.GeoLocation;
            }
            if (location.GeoFence != null)
            {
                loc.GeoFence = location.GeoFence;
            }

            // Vessel KDEs
            loc.VesselID = location.VesselId;
            loc.IMONumber = location.ImoNumber;
            loc.VesselPublicRegistry = location.VesselPublicRegistry;
            loc.VesselFlagState = location.VesselFlagState;

            // Location Classification
            AddClassifications(loc.LocationClassification, location.LocationClassification);

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
        GDSTTradeItem tradeItem = new GDSTTradeItem();

        tradeItem.GTIN = productDef.GetGTIN();
        tradeItem.ShortDescription = new List<OpenTraceability.Models.Common.LanguageString>();
        tradeItem.ShortDescription.Add(new OpenTraceability.Models.Common.LanguageString() { Language = "en-US", Value = productDef.ShortDescription });
        tradeItem.TradeItemConditionCode = productDef.ProductForm;
        tradeItem.FisherySpeciesScientificName = new List<string>();

        if (!string.IsNullOrWhiteSpace(productDef.ScientificName))
        {
            tradeItem.FisherySpeciesScientificName.Add(productDef.ScientificName);
        }

        if (!string.IsNullOrWhiteSpace(productDef.SpeciesCode))
        {
            tradeItem.FisherySpeciesCode = new List<string>() { productDef.SpeciesCode };
        }

        if (productDef.OwnerId != null)
        {
            tradeItem.OwningParty = productDef.GeneratePGLN(productDef.OwnerId);
            tradeItem.InformationProvider = tradeItem.OwningParty;
        }

        // Product Classification
        AddClassifications(tradeItem.ProductClassification, productDef.ProductClassification);

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

    /// <summary>
    /// Parses a comma-delimited classification string and adds one GDST classification per value.
    /// </summary>
    /// <param name="classifications">The classification list to add the values to.</param>
    /// <param name="delimitedValues">The comma-delimited classification values, or null when the source data has none.</param>
    public void AddClassifications(List<GDSTClassification> classifications, string? delimitedValues)
    {
        if (string.IsNullOrWhiteSpace(delimitedValues))
        {
            return;
        }

        foreach (string value in delimitedValues.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0))
        {
            classifications.Add(new GDSTClassification() { Type = "gdst", Value = value });
        }
    }
}
