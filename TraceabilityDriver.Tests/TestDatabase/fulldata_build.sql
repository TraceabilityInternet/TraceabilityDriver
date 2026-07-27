-- =========================================================
-- FullData test database schema.
--
-- Models an internal system whose data, when synced through the
-- fulldata_mapping_sqlserver.json mapping, reproduces the golden
-- capability test document (TraceabilityDriver FullData.json).
--
-- Identifiers (EPCs, GLNs, PGLNs, GTINs) are stored as full URN
-- strings so the mapping passes them through verbatim and the synced
-- output matches the golden document exactly.
-- =========================================================
CREATE SCHEMA fulldata;
GO

-- =========================================================
-- MASTER DATA
-- =========================================================
CREATE TABLE fulldata.Party (
    PartyPgln           nvarchar(200) NOT NULL PRIMARY KEY,
    PartyName           nvarchar(200) NOT NULL
);

CREATE TABLE fulldata.Location (
    LocationGln             nvarchar(200) NOT NULL PRIMARY KEY,
    LocationName            nvarchar(200) NOT NULL,
    OwnerPgln               nvarchar(200) NOT NULL,
    LocationClassification  nvarchar(50) NOT NULL,          -- "vessel" or "land facility"
    VesselId                nvarchar(50) NULL,
    ImoNumber               nvarchar(50) NULL,
    VesselPublicRegistry    nvarchar(200) NULL,
    VesselFlagState         nchar(2) NULL,
    Street1                 nvarchar(200) NULL,
    Street2                 nvarchar(200) NULL,
    City                    nvarchar(100) NULL,
    State                   nvarchar(50) NULL,
    PostalCode              nvarchar(20) NULL,
    CountryCode             nchar(2) NULL,
    GeoLocation             nvarchar(100) NULL,             -- e.g. "geo:37.7749,-122.4194"
    GeoFence                nvarchar(400) NULL              -- JSON array of coordinate pairs
);

CREATE TABLE fulldata.Product (
    ProductGtin             nvarchar(200) NOT NULL PRIMARY KEY,
    OwnerPgln               nvarchar(200) NOT NULL,
    Description             nvarchar(200) NOT NULL,
    ProductForm             nvarchar(30) NULL,
    ScientificName          nvarchar(200) NULL,
    SpeciesCode             nvarchar(20) NULL,
    ProductClassification   nvarchar(100) NULL              -- comma-delimited GDST classifications
);

-- =========================================================
-- CERTIFICATES
-- One row per certificate type; events link to the types they carry.
-- =========================================================
CREATE TABLE fulldata.Certificate (
    CertType            nvarchar(50) NOT NULL PRIMARY KEY,  -- e.g. "harvestCoC"
    Agency              nvarchar(100) NOT NULL,
    Standard            nvarchar(100) NOT NULL,
    CertValue           nvarchar(50) NOT NULL,
    Identification      nvarchar(50) NOT NULL
);

CREATE TABLE fulldata.EventCertificate (
    EventId             int NOT NULL,
    CertType            nvarchar(50) NOT NULL,
    PRIMARY KEY (EventId, CertType)
);

-- =========================================================
-- EVENTS
-- Event times are stored as ISO 8601 strings so the exact fractional
-- seconds of the golden document survive the mapping conversion.
-- =========================================================
CREATE TABLE fulldata.ObjectEvent (
    EventId                     int NOT NULL PRIMARY KEY,
    EventKind                   nvarchar(40) NOT NULL,      -- commissioningevent/shippingevent/receivingevent/decommissioningevent
    EventTimeUtc                nvarchar(40) NOT NULL,
    ReadPoint                   nvarchar(100) NOT NULL,
    Disposition                 nvarchar(40) NULL,
    InfoProviderPgln            nvarchar(200) NOT NULL,
    ProductOwnerPgln            nvarchar(200) NULL,
    LocationGln                 nvarchar(200) NOT NULL,
    HumanWelfarePolicy          nvarchar(40) NULL,
    UnloadingPort               nvarchar(40) NULL,
    ProductionMethod            nvarchar(40) NULL,
    BroodstockSource            nvarchar(40) NULL,
    CountryOfOrigin             nchar(2) NULL,
    SourceLocationGln           nvarchar(200) NULL,
    SourcePartyPgln             nvarchar(200) NULL,
    DestLocationGln             nvarchar(200) NULL,
    DestPartyPgln               nvarchar(200) NULL,
    HasCatchInfo                bit NOT NULL DEFAULT 0,
    CatchArea                   nvarchar(50) NULL,
    EconomicZone                nvarchar(50) NULL,
    GearType                    nvarchar(50) NULL,
    Fip                         nvarchar(50) NULL,
    GpsAvailable                bit NULL,
    RfmoArea                    nvarchar(50) NULL,
    SatelliteTrackingAuthority  nvarchar(50) NULL,
    SubnationalPermitArea       nvarchar(50) NULL,
    VesselTripDate              nvarchar(40) NULL,
    Epc                         nvarchar(200) NOT NULL,
    Quantity                    float NULL,
    Uom                         nvarchar(10) NULL,
    ProductGtin                 nvarchar(200) NULL,
    IsSscc                      bit NOT NULL DEFAULT 0
);

CREATE TABLE fulldata.TransformationEvent (
    EventId                     int NOT NULL PRIMARY KEY,
    EventTimeUtc                nvarchar(40) NOT NULL,
    ReadPoint                   nvarchar(100) NOT NULL,
    Disposition                 nvarchar(40) NULL,
    InfoProviderPgln            nvarchar(200) NOT NULL,
    ProductOwnerPgln            nvarchar(200) NOT NULL,
    LocationGln                 nvarchar(200) NOT NULL,
    HumanWelfarePolicy          nvarchar(40) NULL,
    CountryOfOrigin             nchar(2) NULL,
    ProteinSource               nvarchar(40) NULL,
    AquacultureMethod           nvarchar(40) NULL,
    ProductionMethod            nvarchar(40) NULL
);

CREATE TABLE fulldata.TransformationProduct (
    TransformationProductId     int IDENTITY(1,1) PRIMARY KEY,
    EventId                     int NOT NULL,
    IoType                      nvarchar(10) NOT NULL,      -- "Input" or "Output"
    Epc                         nvarchar(200) NOT NULL,
    Quantity                    float NOT NULL,
    Uom                         nvarchar(10) NOT NULL,
    ProductGtin                 nvarchar(200) NOT NULL
);

CREATE TABLE fulldata.AggregationEvent (
    EventId                     int NOT NULL PRIMARY KEY,
    EventKind                   nvarchar(40) NOT NULL,      -- aggregationevent/disaggregationevent
    EventTimeUtc                nvarchar(40) NOT NULL,
    ReadPoint                   nvarchar(100) NOT NULL,
    Disposition                 nvarchar(40) NULL,
    InfoProviderPgln            nvarchar(200) NOT NULL,
    ProductOwnerPgln            nvarchar(200) NOT NULL,
    LocationGln                 nvarchar(200) NOT NULL,
    ParentEpc                   nvarchar(200) NOT NULL
);

CREATE TABLE fulldata.AggregationChild (
    AggregationChildId          int IDENTITY(1,1) PRIMARY KEY,
    EventId                     int NOT NULL,
    Epc                         nvarchar(200) NOT NULL,
    Quantity                    float NOT NULL,
    Uom                         nvarchar(10) NOT NULL,
    ProductGtin                 nvarchar(200) NOT NULL
);
GO
