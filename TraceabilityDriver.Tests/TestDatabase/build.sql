-- =========================================================
-- SCHEMA
-- =========================================================
CREATE SCHEMA src;
GO

-- =========================================================
-- MASTER DATA
-- =========================================================
CREATE TABLE src.Party (
    PartyId             bigint IDENTITY(1,1) PRIMARY KEY,
    PartyCode           nvarchar(50) NOT NULL UNIQUE,   -- e.g., "OP123", "PLANT9"
    PartyName           nvarchar(200) NOT NULL,
    Gln                 nvarchar(50) NULL,              -- optional if you store real GLNs
    Pgln                nvarchar(50) NULL,              -- optional if you store real PGLNs
    Country             nchar(2) NULL,
    CreatedUtc          datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE src.Location (
    LocationId          bigint IDENTITY(1,1) PRIMARY KEY,
    LocationCode        nvarchar(50) NOT NULL UNIQUE,   -- e.g., "PORT_REYK", "PLANT_1"
    LocationName        nvarchar(200) NOT NULL,
    LocationType        nvarchar(50) NOT NULL,          -- "Port","Plant","Vessel","Warehouse"
    OwnerPartyId        bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    Country             nchar(2) NULL,
    Gln                 nvarchar(50) NULL,
    RegistrationNumber  nvarchar(100) NULL,             -- vessel reg, facility reg, etc.
    CreatedUtc          datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE src.Vessel (
    VesselId            bigint IDENTITY(1,1) PRIMARY KEY,
    VesselCode          nvarchar(50) NOT NULL UNIQUE,   -- internal id
    VesselName          nvarchar(200) NOT NULL,
    FlagCountry         nchar(2) NULL,
    RegistrationNumber  nvarchar(100) NULL,
    VesselLocationId    bigint NULL FOREIGN KEY REFERENCES src.Location(LocationId),
    OwnerPartyId        bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId)
);

CREATE TABLE src.Species (
    SpeciesId           bigint IDENTITY(1,1) PRIMARY KEY,
    ScientificName      nvarchar(200) NOT NULL,
    CommonName          nvarchar(200) NULL,
    FaoCode             nvarchar(20) NULL
);

CREATE TABLE src.ProductDefinition (
    ProductDefinitionId bigint IDENTITY(1,1) PRIMARY KEY,
    OwnerPartyId        bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    Gtin                nvarchar(50) NULL,              -- if you have it; otherwise mapping can GenerateIdentifier(...)
    ShortDescription    nvarchar(200) NOT NULL,
    ProductFormCode     nvarchar(30) NOT NULL,          -- e.g., "RAW","FROZEN","FILLET"
    SpeciesId           bigint NULL FOREIGN KEY REFERENCES src.Species(SpeciesId)
);

CREATE TABLE src.Certificate (
    CertificateId       bigint IDENTITY(1,1) PRIMARY KEY,
    CertificateType     nvarchar(80) NOT NULL,          -- e.g., "fishingAuth","harvestCoC","humanPolicy","harvestCert"
    CertificateNumber   nvarchar(120) NOT NULL,
    IssuerPartyId       bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    ValidFrom           date NULL,
    ValidTo             date NULL
);

-- =========================================================
-- TRACEABILITY UNITS
-- =========================================================
CREATE TABLE src.Lot (
    LotId               bigint IDENTITY(1,1) PRIMARY KEY,
    LotCode             nvarchar(80) NOT NULL UNIQUE,   -- “lot number” in source system
    ProductDefinitionId bigint NOT NULL FOREIGN KEY REFERENCES src.ProductDefinition(ProductDefinitionId),
    OwnerPartyId        bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    ProductionMethod    nvarchar(50) NULL,              -- e.g., "wild", "aquaculture"
    CreatedUtc          datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE src.LogisticUnit (
    LogisticUnitId      bigint IDENTITY(1,1) PRIMARY KEY,
    Sscc                nvarchar(50) NOT NULL UNIQUE,
    OwnerPartyId        bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    CreatedUtc          datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE src.LogisticUnitLot (
    LogisticUnitId      bigint NOT NULL FOREIGN KEY REFERENCES src.LogisticUnit(LogisticUnitId),
    LotId               bigint NOT NULL FOREIGN KEY REFERENCES src.Lot(LotId),
    Quantity            decimal(18,3) NULL,
    Uom                 nvarchar(10) NULL,              -- "KGM", "EA", etc.
    PRIMARY KEY (LogisticUnitId, LotId)
);

-- =========================================================
-- FISHING (Trip -> Activity/Haul -> Catch Lines)
-- =========================================================
CREATE TABLE src.FishingTrip (
    FishingTripId       bigint IDENTITY(1,1) PRIMARY KEY,
    TripNumber          nvarchar(50) NOT NULL UNIQUE,
    VesselId            bigint NOT NULL FOREIGN KEY REFERENCES src.Vessel(VesselId),
    OperatorPartyId     bigint NOT NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    StartUtc            datetime2(0) NOT NULL,
    EndUtc              datetime2(0) NULL
);

CREATE TABLE src.FishingActivity (
    FishingActivityId   bigint IDENTITY(1,1) PRIMARY KEY,
    FishingTripId       bigint NOT NULL FOREIGN KEY REFERENCES src.FishingTrip(FishingTripId),
    ActivityNumber      nvarchar(50) NOT NULL,          -- set/haul id
    EventTimeUtc        datetime2(0) NOT NULL,
    CatchArea           nvarchar(120) NULL,             -- mapping can turn into URNs
    GearTypeCode        nvarchar(50) NULL,              -- maps via Dictionary(...)
    GpsAvailable        bit NOT NULL DEFAULT 0,
    FishingAuthCertId   bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId),
    HumanPolicyCertId   bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId),
    UNIQUE (FishingTripId, ActivityNumber)
);

CREATE TABLE src.FishingCatchLine (
    FishingCatchLineId  bigint IDENTITY(1,1) PRIMARY KEY,
    FishingActivityId   bigint NOT NULL FOREIGN KEY REFERENCES src.FishingActivity(FishingActivityId),
    LotId               bigint NOT NULL FOREIGN KEY REFERENCES src.Lot(LotId),
    Quantity            decimal(18,3) NOT NULL,
    Uom                 nvarchar(10) NOT NULL DEFAULT 'KGM'
);

-- =========================================================
-- LANDING
-- =========================================================
CREATE TABLE src.Landing (
    LandingId           bigint IDENTITY(1,1) PRIMARY KEY,
    LandingNumber       nvarchar(50) NOT NULL UNIQUE,
    VesselId            bigint NOT NULL FOREIGN KEY REFERENCES src.Vessel(VesselId),
    PortLocationId      bigint NOT NULL FOREIGN KEY REFERENCES src.Location(LocationId),
    EventTimeUtc        datetime2(0) NOT NULL,
    InformationProviderPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    ProductOwnerPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    HarvestCertId       bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId),
    HumanPolicyCertId   bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId)
);

CREATE TABLE src.LandingLine (
    LandingLineId       bigint IDENTITY(1,1) PRIMARY KEY,
    LandingId           bigint NOT NULL FOREIGN KEY REFERENCES src.Landing(LandingId),
    LotId               bigint NOT NULL FOREIGN KEY REFERENCES src.Lot(LotId),
    Quantity            decimal(18,3) NOT NULL,
    Uom                 nvarchar(10) NOT NULL DEFAULT 'KGM'
);

-- =========================================================
-- TRANSSHIPMENT
-- =========================================================
CREATE TABLE src.Transshipment (
    TransshipmentId     bigint IDENTITY(1,1) PRIMARY KEY,
    TransshipmentNumber nvarchar(50) NOT NULL UNIQUE,
    FromVesselId        bigint NULL FOREIGN KEY REFERENCES src.Vessel(VesselId),
    ToVesselId          bigint NULL FOREIGN KEY REFERENCES src.Vessel(VesselId),
    AtLocationId        bigint NULL FOREIGN KEY REFERENCES src.Location(LocationId),
    EventTimeUtc        datetime2(0) NOT NULL,
    InformationProviderPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    ProductOwnerPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    TransportNumber     nvarchar(60) NULL,              -- voyage/trip number
    TransportType       nvarchar(30) NULL               -- "vessel"
);

CREATE TABLE src.TransshipmentLine (
    TransshipmentLineId bigint IDENTITY(1,1) PRIMARY KEY,
    TransshipmentId     bigint NOT NULL FOREIGN KEY REFERENCES src.Transshipment(TransshipmentId),
    LotId               bigint NULL FOREIGN KEY REFERENCES src.Lot(LotId),
    LogisticUnitId      bigint NULL FOREIGN KEY REFERENCES src.LogisticUnit(LogisticUnitId),
    Quantity            decimal(18,3) NULL,
    Uom                 nvarchar(10) NULL
);

-- =========================================================
-- PROCESSING / TRANSFORMATION
-- =========================================================
CREATE TABLE src.ProcessingBatch (
    ProcessingBatchId   bigint IDENTITY(1,1) PRIMARY KEY,
    BatchNumber         nvarchar(60) NOT NULL UNIQUE,
    FacilityLocationId  bigint NOT NULL FOREIGN KEY REFERENCES src.Location(LocationId),
    EventTimeUtc        datetime2(0) NOT NULL,
    ProcessingTypeCode  nvarchar(60) NULL,              -- MSC/GDST processing type mapping
    InformationProviderPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    ProductOwnerPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    CocCertId           bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId),
    HumanPolicyCertId   bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId)
);

CREATE TABLE src.ProcessingInput (
    ProcessingInputId   bigint IDENTITY(1,1) PRIMARY KEY,
    ProcessingBatchId   bigint NOT NULL FOREIGN KEY REFERENCES src.ProcessingBatch(ProcessingBatchId),
    LotId               bigint NOT NULL FOREIGN KEY REFERENCES src.Lot(LotId),
    Quantity            decimal(18,3) NOT NULL,
    Uom                 nvarchar(10) NOT NULL DEFAULT 'KGM'
);

CREATE TABLE src.ProcessingOutput (
    ProcessingOutputId  bigint IDENTITY(1,1) PRIMARY KEY,
    ProcessingBatchId   bigint NOT NULL FOREIGN KEY REFERENCES src.ProcessingBatch(ProcessingBatchId),
    LotId               bigint NOT NULL FOREIGN KEY REFERENCES src.Lot(LotId),
    Quantity            decimal(18,3) NOT NULL,
    Uom                 nvarchar(10) NOT NULL DEFAULT 'KGM'
);

-- =========================================================
-- AGGREGATION / DISAGGREGATION / COMMINGLING
-- =========================================================
CREATE TABLE src.AggregationEvent (
    AggregationEventId  bigint IDENTITY(1,1) PRIMARY KEY,
    EventNumber         nvarchar(60) NOT NULL UNIQUE,
    EventType           nvarchar(30) NOT NULL,          -- "ADD","DELETE","COMMINGLE"
    LocationId          bigint NULL FOREIGN KEY REFERENCES src.Location(LocationId),
    EventTimeUtc        datetime2(0) NOT NULL,
    InformationProviderPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    ProductOwnerPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId)
);

CREATE TABLE src.AggregationLine (
    AggregationLineId   bigint IDENTITY(1,1) PRIMARY KEY,
    AggregationEventId  bigint NOT NULL FOREIGN KEY REFERENCES src.AggregationEvent(AggregationEventId),
    ParentLogisticUnitId bigint NOT NULL FOREIGN KEY REFERENCES src.LogisticUnit(LogisticUnitId),
    ChildLogisticUnitId  bigint NULL FOREIGN KEY REFERENCES src.LogisticUnit(LogisticUnitId),
    ChildLotId           bigint NULL FOREIGN KEY REFERENCES src.Lot(LotId),
    Quantity            decimal(18,3) NULL,
    Uom                 nvarchar(10) NULL
);

-- =========================================================
-- SHIPPING / RECEIVING
-- =========================================================
CREATE TABLE src.Shipment (
    ShipmentId          bigint IDENTITY(1,1) PRIMARY KEY,
    ShipmentNumber      nvarchar(60) NOT NULL UNIQUE,
    ShipFromLocationId  bigint NOT NULL FOREIGN KEY REFERENCES src.Location(LocationId),
    ShipToLocationId    bigint NOT NULL FOREIGN KEY REFERENCES src.Location(LocationId),
    EventTimeUtc        datetime2(0) NOT NULL,
    CarrierPartyId      bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    TransportType       nvarchar(30) NULL,              -- "truck","vessel","air"
    TransportVehicleId  nvarchar(80) NULL,
    TransportNumber     nvarchar(80) NULL,
    TransportProviderId nvarchar(80) NULL,              -- optional if not CarrierPartyId
    InformationProviderPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    ProductOwnerPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    CocCertId           bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId),
    HumanPolicyCertId   bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId)
);

CREATE TABLE src.ShipmentLine (
    ShipmentLineId      bigint IDENTITY(1,1) PRIMARY KEY,
    ShipmentId          bigint NOT NULL FOREIGN KEY REFERENCES src.Shipment(ShipmentId),
    LotId               bigint NULL FOREIGN KEY REFERENCES src.Lot(LotId),
    LogisticUnitId      bigint NULL FOREIGN KEY REFERENCES src.LogisticUnit(LogisticUnitId),
    Quantity            decimal(18,3) NULL,
    Uom                 nvarchar(10) NULL
);

CREATE TABLE src.Receipt (
    ReceiptId           bigint IDENTITY(1,1) PRIMARY KEY,
    ReceiptNumber       nvarchar(60) NOT NULL UNIQUE,
    ReceiveAtLocationId bigint NOT NULL FOREIGN KEY REFERENCES src.Location(LocationId),
    EventTimeUtc        datetime2(0) NOT NULL,
    SupplierPartyId     bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    InformationProviderPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    ProductOwnerPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    CocCertId           bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId),
    HumanPolicyCertId   bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId)
);

CREATE TABLE src.ReceiptLine (
    ReceiptLineId       bigint IDENTITY(1,1) PRIMARY KEY,
    ReceiptId           bigint NOT NULL FOREIGN KEY REFERENCES src.Receipt(ReceiptId),
    LotId               bigint NULL FOREIGN KEY REFERENCES src.Lot(LotId),
    LogisticUnitId      bigint NULL FOREIGN KEY REFERENCES src.LogisticUnit(LogisticUnitId),
    Quantity            decimal(18,3) NULL,
    Uom                 nvarchar(10) NULL
);

-- =========================================================
-- STORAGE (MSC-style)
-- =========================================================
CREATE TABLE src.StorageEvent (
    StorageEventId      bigint IDENTITY(1,1) PRIMARY KEY,
    StorageEventNumber  nvarchar(60) NOT NULL UNIQUE,
    LocationId          bigint NOT NULL FOREIGN KEY REFERENCES src.Location(LocationId),
    EventTimeUtc        datetime2(0) NOT NULL,
    InformationProviderPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    ProductOwnerPartyId bigint NULL FOREIGN KEY REFERENCES src.Party(PartyId),
    CocCertId           bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId),
    HumanPolicyCertId   bigint NULL FOREIGN KEY REFERENCES src.Certificate(CertificateId)
);

CREATE TABLE src.StorageLine (
    StorageLineId       bigint IDENTITY(1,1) PRIMARY KEY,
    StorageEventId      bigint NOT NULL FOREIGN KEY REFERENCES src.StorageEvent(StorageEventId),
    LotId               bigint NULL FOREIGN KEY REFERENCES src.Lot(LotId),
    LogisticUnitId      bigint NULL FOREIGN KEY REFERENCES src.LogisticUnit(LogisticUnitId),
    Quantity            decimal(18,3) NULL,
    Uom                 nvarchar(10) NULL
);

-- Helpful indexes for incremental sync
CREATE INDEX IX_FishingActivity_Sync   ON src.FishingActivity(FishingActivityId);
CREATE INDEX IX_Landing_Sync           ON src.Landing(LandingId);
CREATE INDEX IX_Transshipment_Sync     ON src.Transshipment(TransshipmentId);
CREATE INDEX IX_ProcessingBatch_Sync   ON src.ProcessingBatch(ProcessingBatchId);
CREATE INDEX IX_Shipment_Sync          ON src.Shipment(ShipmentId);
CREATE INDEX IX_Receipt_Sync           ON src.Receipt(ReceiptId);
CREATE INDEX IX_AggregationEvent_Sync  ON src.AggregationEvent(AggregationEventId);
GO
