/* =========================================================
   TRACEABILITY DRIVER SOURCE DB - SEED DATA (src schema)
   Rerunnable: wipes src tables then inserts coherent dataset.
   ========================================================= */

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    /* -----------------------------
       1) CLEAR (reverse dependency)
       ----------------------------- */
    DELETE FROM src.StorageLine;
    DELETE FROM src.StorageEvent;

    DELETE FROM src.ReceiptLine;
    DELETE FROM src.Receipt;

    DELETE FROM src.ShipmentLine;
    DELETE FROM src.Shipment;

    DELETE FROM src.AggregationLine;
    DELETE FROM src.AggregationEvent;

    DELETE FROM src.ProcessingOutput;
    DELETE FROM src.ProcessingInput;
    DELETE FROM src.ProcessingBatch;

    DELETE FROM src.TransshipmentLine;
    DELETE FROM src.Transshipment;

    DELETE FROM src.LandingLine;
    DELETE FROM src.Landing;

    DELETE FROM src.FishingCatchLine;
    DELETE FROM src.FishingActivity;
    DELETE FROM src.FishingTrip;

    DELETE FROM src.LogisticUnitLot;
    DELETE FROM src.LogisticUnit;

    DELETE FROM src.Lot;

    DELETE FROM src.Certificate;

    DELETE FROM src.ProductDefinition;
    DELETE FROM src.Species;

    DELETE FROM src.Vessel;

    DELETE FROM src.Location;
    DELETE FROM src.Party;

    /* ------------------------------------
       2) MASTER DATA: Parties, Locations…
       ------------------------------------ */

    -- Parties
    INSERT INTO src.Party (PartyCode, PartyName, Country, Gln, Pgln)
    VALUES
      ('OP001',   'North Sea Fisheries Ltd', 'GB', NULL, NULL),   -- operator / info provider / owner
      ('CARR01',  'BlueWave Logistics',      'GB', NULL, NULL),   -- carrier
      ('PLANT01', 'Harbor Processing Plant', 'GB', NULL, NULL),   -- processor
      ('WARE01',  'ColdStore Warehouse',     'GB', NULL, NULL),   -- warehouse
      ('BUY001',  'Retail Buyer Co',         'GB', NULL, NULL);   -- buyer / receiver

    DECLARE @Party_OP001  bigint = (SELECT PartyId FROM src.Party WHERE PartyCode='OP001');
    DECLARE @Party_CARR01 bigint = (SELECT PartyId FROM src.Party WHERE PartyCode='CARR01');
    DECLARE @Party_PLANT01 bigint = (SELECT PartyId FROM src.Party WHERE PartyCode='PLANT01');
    DECLARE @Party_WARE01  bigint = (SELECT PartyId FROM src.Party WHERE PartyCode='WARE01');
    DECLARE @Party_BUY001  bigint = (SELECT PartyId FROM src.Party WHERE PartyCode='BUY001');

    -- Locations
    INSERT INTO src.Location (LocationCode, LocationName, LocationType, OwnerPartyId, Country, Gln, RegistrationNumber)
    VALUES
      ('VSL_LOC_01', 'FV Northern Star (as location)', 'Vessel',   @Party_OP001,   'GB', NULL, 'GB-FV-NS-001'),
      ('PORT_01',    'Port of Grimsby',               'Port',     NULL,          'GB', NULL, NULL),
      ('PLANT_01',   'Harbor Processing Plant',       'Plant',    @Party_PLANT01, 'GB', NULL, 'PLANT-GB-01'),
      ('WARE_01',    'ColdStore Warehouse',           'Warehouse',@Party_WARE01,  'GB', NULL, 'WARE-GB-01'),
      ('BUY_DC_01',  'Retail Buyer DC',               'Warehouse',@Party_BUY001,  'GB', NULL, 'DC-GB-01');

    DECLARE @Loc_Vessel bigint = (SELECT LocationId FROM src.Location WHERE LocationCode='VSL_LOC_01');
    DECLARE @Loc_Port   bigint = (SELECT LocationId FROM src.Location WHERE LocationCode='PORT_01');
    DECLARE @Loc_Plant  bigint = (SELECT LocationId FROM src.Location WHERE LocationCode='PLANT_01');
    DECLARE @Loc_Ware   bigint = (SELECT LocationId FROM src.Location WHERE LocationCode='WARE_01');
    DECLARE @Loc_BuyDC  bigint = (SELECT LocationId FROM src.Location WHERE LocationCode='BUY_DC_01');

    -- Vessel
    INSERT INTO src.Vessel (VesselCode, VesselName, FlagCountry, RegistrationNumber, VesselLocationId, OwnerPartyId)
    VALUES ('VSL001', 'FV Northern Star', 'GB', 'GB-FV-NS-001', @Loc_Vessel, @Party_OP001);

    DECLARE @Vessel_VSL001 bigint = (SELECT VesselId FROM src.Vessel WHERE VesselCode='VSL001');

    -- Species
    INSERT INTO src.Species (ScientificName, CommonName, FaoCode)
    VALUES
      ('Gadus morhua', 'Atlantic cod', 'COD'),
      ('Melanogrammus aeglefinus', 'Haddock', 'HAD');

    DECLARE @Species_Cod bigint = (SELECT SpeciesId FROM src.Species WHERE ScientificName='Gadus morhua');
    DECLARE @Species_Had bigint = (SELECT SpeciesId FROM src.Species WHERE ScientificName='Melanogrammus aeglefinus');

    -- Product definitions (raw whole fish -> processed fillet)
    INSERT INTO src.ProductDefinition (OwnerPartyId, Gtin, ShortDescription, ProductFormCode, SpeciesId)
    VALUES
      (@Party_OP001,    '00012345600012', 'Atlantic Cod - Whole (Raw)',   'RAW',    @Species_Cod),
      (@Party_PLANT01,  '00012345600029', 'Atlantic Cod - Fillet (Chilled)','FILLET',@Species_Cod),
      (@Party_OP001,    '00012345600036', 'Haddock - Whole (Raw)',        'RAW',    @Species_Had);

    DECLARE @PD_CodRaw   bigint = (SELECT ProductDefinitionId FROM src.ProductDefinition WHERE ShortDescription LIKE 'Atlantic Cod - Whole%');
    DECLARE @PD_CodFillet bigint = (SELECT ProductDefinitionId FROM src.ProductDefinition WHERE ShortDescription LIKE 'Atlantic Cod - Fillet%');
    DECLARE @PD_HadRaw   bigint = (SELECT ProductDefinitionId FROM src.ProductDefinition WHERE ShortDescription LIKE 'Haddock - Whole%');

    /* ------------------------------------
       3) Certificates (for mapping to KDEs)
       ------------------------------------ */
    INSERT INTO src.Certificate (CertificateType, CertificateNumber, IssuerPartyId, ValidFrom, ValidTo)
    VALUES
      ('fishingAuth',  'FA-GB-2026-0001', @Party_OP001,  '2026-01-01', '2026-12-31'),
      ('harvestCoC',   'COC-GB-PLANT-01', @Party_PLANT01,'2025-01-01', '2027-12-31'),
      ('humanPolicy',  'HP-GB-0009',      NULL,         '2025-01-01', '2027-12-31'),
      ('harvestCert',  'HC-GB-7777',      NULL,         '2025-01-01', '2027-12-31');

    DECLARE @Cert_FishingAuth bigint = (SELECT CertificateId FROM src.Certificate WHERE CertificateType='fishingAuth');
    DECLARE @Cert_CoC         bigint = (SELECT CertificateId FROM src.Certificate WHERE CertificateType='harvestCoC');
    DECLARE @Cert_Human       bigint = (SELECT CertificateId FROM src.Certificate WHERE CertificateType='humanPolicy');
    DECLARE @Cert_Harvest     bigint = (SELECT CertificateId FROM src.Certificate WHERE CertificateType='harvestCert');

    /* ------------------------------------
       4) Lots (raw catch + processed outputs)
       ------------------------------------ */
    INSERT INTO src.Lot (LotCode, ProductDefinitionId, OwnerPartyId, ProductionMethod)
    VALUES
      ('LOT-COD-RAW-0001', @PD_CodRaw,   @Party_OP001,   'wild'),
      ('LOT-HAD-RAW-0001', @PD_HadRaw,   @Party_OP001,   'wild'),
      ('LOT-COD-FLT-0001', @PD_CodFillet,@Party_PLANT01, 'wild');

    DECLARE @Lot_CodRaw bigint = (SELECT LotId FROM src.Lot WHERE LotCode='LOT-COD-RAW-0001');
    DECLARE @Lot_HadRaw bigint = (SELECT LotId FROM src.Lot WHERE LotCode='LOT-HAD-RAW-0001');
    DECLARE @Lot_CodFlt bigint = (SELECT LotId FROM src.Lot WHERE LotCode='LOT-COD-FLT-0001');

    /* ------------------------------------
       5) Logistic Units (SSCCs) + contents
       ------------------------------------ */
    INSERT INTO src.LogisticUnit (Sscc, OwnerPartyId)
    VALUES
      ('000000000000000001', @Party_PLANT01),  -- pallet at plant
      ('000000000000000002', @Party_PLANT01);  -- second pallet (for commingle/disagg tests)

    DECLARE @SSCC_1 bigint = (SELECT LogisticUnitId FROM src.LogisticUnit WHERE Sscc='000000000000000001');
    DECLARE @SSCC_2 bigint = (SELECT LogisticUnitId FROM src.LogisticUnit WHERE Sscc='000000000000000002');

    -- Put fillet lot into SSCC_1
    INSERT INTO src.LogisticUnitLot (LogisticUnitId, LotId, Quantity, Uom)
    VALUES (@SSCC_1, @Lot_CodFlt, 500.000, 'KGM');

    -- Put some raw lots into SSCC_2 for commingling examples (optional)
    INSERT INTO src.LogisticUnitLot (LogisticUnitId, LotId, Quantity, Uom)
    VALUES
      (@SSCC_2, @Lot_CodRaw, 200.000, 'KGM'),
      (@SSCC_2, @Lot_HadRaw, 150.000, 'KGM');

    /* ------------------------------------
       6) Fishing Trip + Activities + Catch
       ------------------------------------ */
    INSERT INTO src.FishingTrip (TripNumber, VesselId, OperatorPartyId, StartUtc, EndUtc)
    VALUES ('TRIP-2026-0001', @Vessel_VSL001, @Party_OP001, '2026-01-02T06:00:00', '2026-01-03T18:00:00');

    DECLARE @TripId bigint = (SELECT FishingTripId FROM src.FishingTrip WHERE TripNumber='TRIP-2026-0001');

    -- Activity 1: catch cod
    INSERT INTO src.FishingActivity (
        FishingTripId, ActivityNumber, EventTimeUtc,
        CatchArea, GearTypeCode, GpsAvailable,
        FishingAuthCertId, HumanPolicyCertId
    )
    VALUES
      (@TripId, 'SET-001', '2026-01-02T10:30:00',
       'urn:example:area:01', 'GEAR1', 1,
       @Cert_FishingAuth, @Cert_Human);

    DECLARE @Act1 bigint = (SELECT FishingActivityId FROM src.FishingActivity WHERE FishingTripId=@TripId AND ActivityNumber='SET-001');

    INSERT INTO src.FishingCatchLine (FishingActivityId, LotId, Quantity, Uom)
    VALUES
      (@Act1, @Lot_CodRaw, 1000.000, 'KGM');

    -- Activity 2: catch haddock
    INSERT INTO src.FishingActivity (
        FishingTripId, ActivityNumber, EventTimeUtc,
        CatchArea, GearTypeCode, GpsAvailable,
        FishingAuthCertId, HumanPolicyCertId
    )
    VALUES
      (@TripId, 'SET-002', '2026-01-03T09:15:00',
       'urn:example:area:02', 'GEAR9_9', 1,
       @Cert_FishingAuth, @Cert_Human);

    DECLARE @Act2 bigint = (SELECT FishingActivityId FROM src.FishingActivity WHERE FishingTripId=@TripId AND ActivityNumber='SET-002');

    INSERT INTO src.FishingCatchLine (FishingActivityId, LotId, Quantity, Uom)
    VALUES
      (@Act2, @Lot_HadRaw, 800.000, 'KGM');

    /* ------------------------------------
       7) Landing (cod + haddock)
       ------------------------------------ */
    INSERT INTO src.Landing (
        LandingNumber, VesselId, PortLocationId, EventTimeUtc,
        InformationProviderPartyId, ProductOwnerPartyId,
        HarvestCertId, HumanPolicyCertId
    )
    VALUES
      ('LAND-2026-0001', @Vessel_VSL001, @Loc_Port, '2026-01-03T19:00:00',
       @Party_OP001, @Party_OP001,
       @Cert_Harvest, @Cert_Human);

    DECLARE @LandingId bigint = (SELECT LandingId FROM src.Landing WHERE LandingNumber='LAND-2026-0001');

    INSERT INTO src.LandingLine (LandingId, LotId, Quantity, Uom)
    VALUES
      (@LandingId, @Lot_CodRaw, 950.000, 'KGM'),
      (@LandingId, @Lot_HadRaw, 780.000, 'KGM');

    /* ------------------------------------
       8) Transshipment (move some cod raw to plant)
       ------------------------------------ */
    INSERT INTO src.Transshipment (
        TransshipmentNumber, FromVesselId, ToVesselId, AtLocationId, EventTimeUtc,
        InformationProviderPartyId, ProductOwnerPartyId,
        TransportNumber, TransportType
    )
    VALUES
      ('TS-2026-0001', @Vessel_VSL001, NULL, @Loc_Port, '2026-01-03T20:30:00',
       @Party_OP001, @Party_OP001,
       'VOY-TS-01', 'vessel');

    DECLARE @TSId bigint = (SELECT TransshipmentId FROM src.Transshipment WHERE TransshipmentNumber='TS-2026-0001');

    INSERT INTO src.TransshipmentLine (TransshipmentId, LotId, LogisticUnitId, Quantity, Uom)
    VALUES
      (@TSId, @Lot_CodRaw, NULL, 500.000, 'KGM');

    /* ------------------------------------
       9) Processing batch (transform cod raw -> cod fillet)
       ------------------------------------ */
    INSERT INTO src.ProcessingBatch (
        BatchNumber, FacilityLocationId, EventTimeUtc, ProcessingTypeCode,
        InformationProviderPartyId, ProductOwnerPartyId,
        CocCertId, HumanPolicyCertId
    )
    VALUES
      ('PB-2026-0001', @Loc_Plant, '2026-01-04T08:00:00', 'FILLETING',
       @Party_PLANT01, @Party_PLANT01,
       @Cert_CoC, @Cert_Human);

    DECLARE @BatchId bigint = (SELECT ProcessingBatchId FROM src.ProcessingBatch WHERE BatchNumber='PB-2026-0001');

    INSERT INTO src.ProcessingInput (ProcessingBatchId, LotId, Quantity, Uom)
    VALUES
      (@BatchId, @Lot_CodRaw, 500.000, 'KGM');

    INSERT INTO src.ProcessingOutput (ProcessingBatchId, LotId, Quantity, Uom)
    VALUES
      (@BatchId, @Lot_CodFlt, 450.000, 'KGM');

    /* ------------------------------------
       10) Aggregation ADD (put output lot into SSCC_1)
       + Commingling example SSCC_2 is already mixed
       ------------------------------------ */
    INSERT INTO src.AggregationEvent (
        EventNumber, EventType, LocationId, EventTimeUtc,
        InformationProviderPartyId, ProductOwnerPartyId
    )
    VALUES
      ('AGG-2026-ADD-0001', 'ADD', @Loc_Plant, '2026-01-04T10:00:00',
       @Party_PLANT01, @Party_PLANT01);

    DECLARE @AggAddId bigint = (SELECT AggregationEventId FROM src.AggregationEvent WHERE EventNumber='AGG-2026-ADD-0001');

    -- Represent: Parent SSCC_1 contains child lot cod fillet
    INSERT INTO src.AggregationLine (
        AggregationEventId, ParentLogisticUnitId, ChildLogisticUnitId, ChildLotId, Quantity, Uom
    )
    VALUES
      (@AggAddId, @SSCC_1, NULL, @Lot_CodFlt, 450.000, 'KGM');

    -- Optional "COMMINGLE" event using SSCC_2
    INSERT INTO src.AggregationEvent (
        EventNumber, EventType, LocationId, EventTimeUtc,
        InformationProviderPartyId, ProductOwnerPartyId
    )
    VALUES
      ('AGG-2026-COM-0001', 'COMMINGLE', @Loc_Ware, '2026-01-04T14:00:00',
       @Party_WARE01, @Party_WARE01);

    DECLARE @AggComId bigint = (SELECT AggregationEventId FROM src.AggregationEvent WHERE EventNumber='AGG-2026-COM-0001');

    INSERT INTO src.AggregationLine (
        AggregationEventId, ParentLogisticUnitId, ChildLogisticUnitId, ChildLotId, Quantity, Uom
    )
    VALUES
      (@AggComId, @SSCC_2, NULL, @Lot_CodRaw, 200.000, 'KGM'),
      (@AggComId, @SSCC_2, NULL, @Lot_HadRaw, 150.000, 'KGM');

    -- Optional DISAGG for core disaggregation tests
    INSERT INTO src.AggregationEvent (
        EventNumber, EventType, LocationId, EventTimeUtc,
        InformationProviderPartyId, ProductOwnerPartyId
    )
    VALUES
      ('AGG-2026-DEL-0001', 'DELETE', @Loc_Ware, '2026-01-04T15:00:00',
       @Party_WARE01, @Party_WARE01);

    DECLARE @AggDelId bigint = (SELECT AggregationEventId FROM src.AggregationEvent WHERE EventNumber='AGG-2026-DEL-0001');

    INSERT INTO src.AggregationLine (
        AggregationEventId, ParentLogisticUnitId, ChildLogisticUnitId, ChildLotId, Quantity, Uom
    )
    VALUES
      (@AggDelId, @SSCC_2, NULL, @Lot_HadRaw, 150.000, 'KGM');

    /* ------------------------------------
       11) Shipment (ship SSCC_1 from plant to buyer DC)
       ------------------------------------ */
    INSERT INTO src.Shipment (
        ShipmentNumber, ShipFromLocationId, ShipToLocationId, EventTimeUtc,
        CarrierPartyId,
        TransportType, TransportVehicleId, TransportNumber, TransportProviderId,
        InformationProviderPartyId, ProductOwnerPartyId,
        CocCertId, HumanPolicyCertId
    )
    VALUES
      ('SHIP-2026-0001', @Loc_Plant, @Loc_BuyDC, '2026-01-05T07:00:00',
       @Party_CARR01,
       'truck', 'TRUCK-77', 'BOL-9001', 'CARR01',
       @Party_PLANT01, @Party_PLANT01,
       @Cert_CoC, @Cert_Human);

    DECLARE @ShipId bigint = (SELECT ShipmentId FROM src.Shipment WHERE ShipmentNumber='SHIP-2026-0001');

    INSERT INTO src.ShipmentLine (ShipmentId, LotId, LogisticUnitId, Quantity, Uom)
    VALUES
      (@ShipId, NULL, @SSCC_1, NULL, NULL),      -- shipping SSCC as the traceability unit
      (@ShipId, @Lot_CodFlt, NULL, 450.000, 'KGM'); -- and/or explicit lot line (lets you test both patterns)

    /* ------------------------------------
       12) Receipt (receive at buyer DC)
       ------------------------------------ */
    INSERT INTO src.Receipt (
        ReceiptNumber, ReceiveAtLocationId, EventTimeUtc,
        SupplierPartyId,
        InformationProviderPartyId, ProductOwnerPartyId,
        CocCertId, HumanPolicyCertId
    )
    VALUES
      ('RCV-2026-0001', @Loc_BuyDC, '2026-01-05T13:30:00',
       @Party_PLANT01,
       @Party_BUY001, @Party_BUY001,
       @Cert_CoC, @Cert_Human);

    DECLARE @RcvId bigint = (SELECT ReceiptId FROM src.Receipt WHERE ReceiptNumber='RCV-2026-0001');

    INSERT INTO src.ReceiptLine (ReceiptId, LotId, LogisticUnitId, Quantity, Uom)
    VALUES
      (@RcvId, NULL, @SSCC_1, NULL, NULL),
      (@RcvId, @Lot_CodFlt, NULL, 450.000, 'KGM');

    /* ------------------------------------
       13) Storage event (MSC storage)
       ------------------------------------ */
    INSERT INTO src.StorageEvent (
        StorageEventNumber, LocationId, EventTimeUtc,
        InformationProviderPartyId, ProductOwnerPartyId,
        CocCertId, HumanPolicyCertId
    )
    VALUES
      ('STO-2026-0001', @Loc_BuyDC, '2026-01-05T14:30:00',
       @Party_BUY001, @Party_BUY001,
       @Cert_CoC, @Cert_Human);

    DECLARE @StoId bigint = (SELECT StorageEventId FROM src.StorageEvent WHERE StorageEventNumber='STO-2026-0001');

    INSERT INTO src.StorageLine (StorageEventId, LotId, LogisticUnitId, Quantity, Uom)
    VALUES
      (@StoId, @Lot_CodFlt, NULL, 450.000, 'KGM');

    COMMIT;

    PRINT 'Seed complete. Inserted master data + events for fishing, landing, transshipment, processing/transformation, aggregation/commingle/disagg, shipping, receiving, storage.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    DECLARE @msg nvarchar(4000) = ERROR_MESSAGE();
    RAISERROR('Seed failed: %s', 16, 1, @msg);
END CATCH;
