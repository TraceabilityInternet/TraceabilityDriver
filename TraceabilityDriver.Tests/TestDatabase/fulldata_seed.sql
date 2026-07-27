-- =========================================================
-- FullData test database seed.
--
-- Every row mirrors the golden capability test document
-- (TraceabilityDriver FullData.json) exactly: 24 events across the
-- supply chain (vessel fishing, hatchery, feed mill, fish farm,
-- processor, importer) plus the parties, locations, products, and
-- certificates they reference.
-- =========================================================

-- =========================================================
-- PARTIES
-- =========================================================
INSERT INTO fulldata.Party (PartyPgln, PartyName) VALUES
('urn:gdst:example.org:party:solution.fisherman', 'Fisherman'),
('urn:gdst:example.org:party:solution.hatchery', 'Hatchery Co.'),
('urn:gdst:example.org:party:solution.feedmill', 'Feed Mill Co.'),
('urn:gdst:example.org:party:solution.fishfarm', 'Fish Farm Co.'),
('urn:gdst:example.org:party:solution.processor', 'Processor Co.'),
('urn:gdst:example.org:party:solution.importer', 'Importer Co.');
GO

-- =========================================================
-- LOCATIONS
-- =========================================================
INSERT INTO fulldata.Location (LocationGln, LocationName, OwnerPgln, LocationClassification, VesselId, ImoNumber, VesselPublicRegistry, VesselFlagState, Street1, Street2, City, State, PostalCode, CountryCode, GeoLocation, GeoFence) VALUES
('urn:gdst:example.org:location:loc:solution.vessel1', 'Vessel #1', 'urn:gdst:example.org:party:solution.fisherman', 'vessel', 'VESSEL1', 'IMO1234567', 'https://example.org/vessels/VESSEL1', 'US', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
('urn:gdst:example.org:location:loc:solution.vessel2', 'Vessel #2', 'urn:gdst:example.org:party:solution.processor', 'vessel', 'VESSEL2', 'IMO1234567', 'https://example.org/vessels/VESSEL2', 'US', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
('urn:gdst:example.org:location:loc:solution.port', 'Port', 'urn:gdst:example.org:party:solution.processor', 'land facility', NULL, NULL, NULL, NULL, '123 Port St', 'Suite 100', 'San Francisco', 'CA', '94111', 'US', NULL, NULL),
('urn:gdst:example.org:location:loc:solution.hatchery', 'Hatchery', 'urn:gdst:example.org:party:solution.hatchery', 'land facility', NULL, NULL, NULL, NULL, '456 Hatchery Rd', NULL, 'San Francisco', 'CA', '94111', 'US', NULL, NULL),
('urn:gdst:example.org:location:loc:solution.feedmill', 'Feed Mill', 'urn:gdst:example.org:party:solution.feedmill', 'land facility', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'geo:37.7749,-122.4194', NULL),
('urn:gdst:example.org:location:loc:solution.fishfarm', 'Fish Farm', 'urn:gdst:example.org:party:solution.fishfarm', 'land facility', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, '[[-122.4194,37.7749],[-122.418,37.7749],[-122.418,37.776],[-122.4194,37.776],[-122.4194,37.7749]]'),
('urn:gdst:example.org:location:loc:solution.processorplant', 'Processor Plant', 'urn:gdst:example.org:party:solution.processor', 'land facility', NULL, NULL, NULL, NULL, '321 Cannery Row', NULL, 'San Francisco', 'CA', '94111', 'US', NULL, NULL),
('urn:gdst:example.org:location:loc:solution.importerwarehouse', 'Importer Warehouse', 'urn:gdst:example.org:party:solution.importer', 'land facility', NULL, NULL, NULL, NULL, '654 Harbor Blvd', NULL, 'San Francisco', 'CA', '94111', 'US', NULL, NULL);
GO

-- =========================================================
-- PRODUCTS
-- =========================================================
INSERT INTO fulldata.Product (ProductGtin, OwnerPgln, Description, ProductForm, ScientificName, SpeciesCode, ProductClassification) VALUES
('urn:gdst:example.org:product:class:solution.wildfish', 'urn:gdst:example.org:party:solution.fisherman', 'Wild Yellowfin Tuna', 'OTH', 'Thunnus albacares', 'YFT', 'seafood, wildcaught'),
('urn:gdst:example.org:product:class:solution.fryling', 'urn:gdst:example.org:party:solution.hatchery', 'Tuna Fryling', 'OTH', 'Thunnus albacares', 'YFT', 'seafood, developing'),
('urn:gdst:example.org:product:class:solution.soy', 'urn:gdst:example.org:party:solution.feedmill', 'Soy Meal', NULL, NULL, NULL, NULL),
('urn:gdst:example.org:product:class:solution.feed', 'urn:gdst:example.org:party:solution.feedmill', 'Fish Feed', NULL, NULL, NULL, 'feed'),
('urn:gdst:example.org:product:class:solution.farmedfish', 'urn:gdst:example.org:party:solution.fishfarm', 'Farmed Yellowfin Tuna', 'OTH', 'Thunnus albacares', 'YFT', 'seafood, mature'),
('urn:gdst:example.org:product:class:solution.processedwild', 'urn:gdst:example.org:party:solution.processor', 'Processed Wild Tuna', 'OTH', 'Thunnus albacares', 'YFT', 'seafood, processed'),
('urn:gdst:example.org:product:class:solution.processedfarmed', 'urn:gdst:example.org:party:solution.processor', 'Processed Farmed Tuna', 'OTH', 'Thunnus albacares', 'YFT', 'seafood, processed');
GO

-- =========================================================
-- CERTIFICATES
-- =========================================================
INSERT INTO fulldata.Certificate (CertType, Agency, Standard, CertValue, Identification) VALUES
('harvestCoC', 'CoC Agency', 'Generic Chain of Custody Standard', 'abc123', 'COC-0001'),
('processorLicense', 'Processor Agency', 'Processor License Standard', 'def456', 'PROC-0002'),
('harvestCert', 'Harvest Agency', 'Harvest Certificate Standard', 'ghi789', 'HARV-0003'),
('landingAuth', 'Landing Auth Agency', 'Landing Authorization Standard', 'jkl012', 'LAND-0004'),
('legalAuth', 'Legal Auth Agency', 'Legal Authorization Standard', 'mno345', 'LEGL-0005'),
('humanPolicy', 'Human Policy Agency', 'Human Welfare Policy Standard', 'pqr678', 'HUMN-0006'),
('transshipmentAuth', 'Transshipment Auth Agency', 'Transshipment Authorization Standard', 'stu901', 'TRANS-0007'),
('fishingAuth', 'Fishing Auth Agency', 'Fishing Authorization Standard', 'vwx234', 'FISH-0008');
GO

INSERT INTO fulldata.EventCertificate (EventId, CertType) VALUES
(1, 'harvestCert'), (1, 'humanPolicy'), (1, 'fishingAuth'),
(2, 'harvestCoC'), (2, 'processorLicense'), (2, 'humanPolicy'),
(3, 'harvestCoC'), (3, 'humanPolicy'), (3, 'transshipmentAuth'),
(4, 'harvestCoC'), (4, 'humanPolicy'), (4, 'transshipmentAuth'),
(5, 'harvestCert'), (5, 'landingAuth'), (5, 'humanPolicy'),
(6, 'harvestCoC'),
(7, 'harvestCoC'),
(9, 'harvestCoC'), (9, 'processorLicense'),
(10, 'harvestCoC'),
(11, 'harvestCoC'),
(12, 'harvestCert'), (12, 'humanPolicy'), (12, 'legalAuth'),
(13, 'harvestCoC'),
(14, 'harvestCoC'),
(15, 'harvestCoC'), (15, 'humanPolicy'), (15, 'harvestCert'), (15, 'legalAuth'),
(16, 'harvestCoC'),
(17, 'harvestCoC'),
(18, 'harvestCoC'), (18, 'processorLicense'),
(19, 'harvestCoC'), (19, 'processorLicense'),
(21, 'harvestCoC'),
(22, 'harvestCoC'),
(23, 'harvestCoC'),
(24, 'harvestCoC');
GO

-- =========================================================
-- OBJECT EVENTS
-- =========================================================

-- Event 1: fishing commission on Vessel #1 (wildfish.lot1).
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, ProductOwnerPgln, LocationGln, HumanWelfarePolicy, ProductionMethod, HasCatchInfo, CatchArea, EconomicZone, GearType, Fip, GpsAvailable, RfmoArea, SatelliteTrackingAuthority, SubnationalPermitArea, VesselTripDate, Epc, Quantity, Uom, ProductGtin)
VALUES (1, 'commissioningevent', '2026-04-22T14:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.fisherman', 'urn:gdst:example.org:party:solution.fisherman', 'urn:gdst:example.org:location:loc:solution.vessel1', '3p', 'MARINE_FISHERY', 1, 'urn:gdst:fao:27.1', 'urn:gdst:eez:usa', 'urn:gdst:gear:1.1', 'fip-1', 1, 'urn:gdst:rfmo:cecaf', 'sat-auth', 'subnat-1', '2026-04-22T13:44:36.6820658+00:00', 'urn:gdst:example.org:product:lot:class:solution.wildfish.lot1', 10000, 'KGM', 'urn:gdst:example.org:product:class:solution.wildfish');

-- Event 3: ship wildfish.lot2 from Vessel #1 to Vessel #2.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, LocationGln, UnloadingPort, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (3, 'shippingevent', '2026-04-22T16:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'in_transit', 'urn:gdst:example.org:party:solution.fisherman', 'urn:gdst:example.org:location:loc:solution.vessel1', 'PORTSD', 'urn:gdst:example.org:location:loc:solution.vessel1', 'urn:gdst:example.org:party:solution.fisherman', 'urn:gdst:example.org:location:loc:solution.vessel2', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:product:lot:class:solution.wildfish.lot2', 9500, 'KGM', 'urn:gdst:example.org:product:class:solution.wildfish');

-- Event 4: receive wildfish.lot2 on Vessel #2 (transshipment).
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, LocationGln, UnloadingPort, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (4, 'receivingevent', '2026-04-22T17:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.vessel2', 'PORTSD', 'urn:gdst:example.org:location:loc:solution.vessel1', 'urn:gdst:example.org:party:solution.fisherman', 'urn:gdst:example.org:location:loc:solution.vessel2', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:product:lot:class:solution.wildfish.lot2', 9500, 'KGM', 'urn:gdst:example.org:product:class:solution.wildfish');

-- Event 5: land wildfish.lot2 at the Port.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, LocationGln, HumanWelfarePolicy, UnloadingPort, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (5, 'receivingevent', '2026-04-22T18:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.port', '3p', 'PORTSD', 'urn:gdst:example.org:location:loc:solution.vessel2', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.port', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:product:lot:class:solution.wildfish.lot2', 9500, 'KGM', 'urn:gdst:example.org:product:class:solution.wildfish');

-- Event 6: ship wildfish.lot2 from the Port to the Processor Plant.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, LocationGln, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (6, 'shippingevent', '2026-04-22T19:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'in_transit', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.port', 'urn:gdst:example.org:location:loc:solution.port', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:product:lot:class:solution.wildfish.lot2', 9500, 'KGM', 'urn:gdst:example.org:product:class:solution.wildfish');

-- Event 7: receive wildfish.lot2 at the Processor Plant.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, LocationGln, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (7, 'receivingevent', '2026-04-22T20:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:gdst:example.org:location:loc:solution.port', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:product:lot:class:solution.wildfish.lot2', 9500, 'KGM', 'urn:gdst:example.org:product:class:solution.wildfish');

-- Event 8: commission soy.lot1 at the Feed Mill.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, ProductOwnerPgln, LocationGln, CountryOfOrigin, Epc, Quantity, Uom, ProductGtin)
VALUES (8, 'commissioningevent', '2026-04-22T21:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.feedmill', 'urn:gdst:example.org:party:solution.feedmill', 'urn:gdst:example.org:location:loc:solution.feedmill', 'US', 'urn:gdst:example.org:product:lot:class:solution.soy.lot1', 6000, 'KGM', 'urn:gdst:example.org:product:class:solution.soy');

-- Event 10: ship feed.lot1 from the Feed Mill to the Fish Farm.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, LocationGln, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (10, 'shippingevent', '2026-04-22T23:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'in_transit', 'urn:gdst:example.org:party:solution.feedmill', 'urn:gdst:example.org:location:loc:solution.feedmill', 'urn:gdst:example.org:location:loc:solution.feedmill', 'urn:gdst:example.org:party:solution.feedmill', 'urn:gdst:example.org:location:loc:solution.fishfarm', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:product:lot:class:solution.feed.lot1', 5000, 'KGM', 'urn:gdst:example.org:product:class:solution.feed');

-- Event 11: receive feed.lot1 at the Fish Farm.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, LocationGln, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (11, 'receivingevent', '2026-04-23T00:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:location:loc:solution.fishfarm', 'urn:gdst:example.org:location:loc:solution.feedmill', 'urn:gdst:example.org:party:solution.feedmill', 'urn:gdst:example.org:location:loc:solution.fishfarm', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:product:lot:class:solution.feed.lot1', 5000, 'KGM', 'urn:gdst:example.org:product:class:solution.feed');

-- Event 12: commission fryling.lot1 at the Hatchery.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, ProductOwnerPgln, LocationGln, HumanWelfarePolicy, ProductionMethod, BroodstockSource, Epc, Quantity, Uom, ProductGtin)
VALUES (12, 'commissioningevent', '2026-04-23T01:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.hatchery', 'urn:gdst:example.org:party:solution.hatchery', 'urn:gdst:example.org:location:loc:solution.hatchery', '3p', 'AQUACULTURE', 'domestic', 'urn:gdst:example.org:product:lot:class:solution.fryling.lot1', 1000, 'KGM', 'urn:gdst:example.org:product:class:solution.fryling');

-- Event 13: ship fryling.lot1 from the Hatchery to the Fish Farm.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, LocationGln, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (13, 'shippingevent', '2026-04-23T02:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'in_transit', 'urn:gdst:example.org:party:solution.hatchery', 'urn:gdst:example.org:location:loc:solution.hatchery', 'urn:gdst:example.org:location:loc:solution.hatchery', 'urn:gdst:example.org:party:solution.hatchery', 'urn:gdst:example.org:location:loc:solution.fishfarm', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:product:lot:class:solution.fryling.lot1', 1000, 'KGM', 'urn:gdst:example.org:product:class:solution.fryling');

-- Event 14: receive fryling.lot1 at the Fish Farm.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, LocationGln, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (14, 'receivingevent', '2026-04-23T03:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:location:loc:solution.fishfarm', 'urn:gdst:example.org:location:loc:solution.hatchery', 'urn:gdst:example.org:party:solution.hatchery', 'urn:gdst:example.org:location:loc:solution.fishfarm', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:product:lot:class:solution.fryling.lot1', 1000, 'KGM', 'urn:gdst:example.org:product:class:solution.fryling');

-- Event 16: ship farmedfish.lot1 from the Fish Farm to the Processor Plant.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, LocationGln, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (16, 'shippingevent', '2026-04-23T05:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'in_transit', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:location:loc:solution.fishfarm', 'urn:gdst:example.org:location:loc:solution.fishfarm', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:product:lot:class:solution.farmedfish.lot1', 6000, 'KGM', 'urn:gdst:example.org:product:class:solution.farmedfish');

-- Event 17: receive farmedfish.lot1 at the Processor Plant.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, LocationGln, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, Quantity, Uom, ProductGtin)
VALUES (17, 'receivingevent', '2026-04-23T06:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:gdst:example.org:location:loc:solution.fishfarm', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:product:lot:class:solution.farmedfish.lot1', 6000, 'KGM', 'urn:gdst:example.org:product:class:solution.farmedfish');

-- Event 20: destroy 100 KGM of processedwild.final1 at the Processor Plant.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, ProductOwnerPgln, LocationGln, Epc, Quantity, Uom, ProductGtin)
VALUES (20, 'decommissioningevent', '2026-04-23T09:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'inactive', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:gdst:example.org:product:lot:class:solution.processedwild.final1', 100, 'KGM', 'urn:gdst:example.org:product:class:solution.processedwild');

-- Event 22: ship the SSCC from the Processor Plant to the Importer Warehouse.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, LocationGln, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, IsSscc)
VALUES (22, 'shippingevent', '2026-04-23T11:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'in_transit', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.importerwarehouse', 'urn:gdst:example.org:party:solution.importer', 'urn:epc:id:sscc:08600031303.solution1', 1);

-- Event 23: receive the SSCC at the Importer Warehouse.
INSERT INTO fulldata.ObjectEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, LocationGln, SourceLocationGln, SourcePartyPgln, DestLocationGln, DestPartyPgln, Epc, IsSscc)
VALUES (23, 'receivingevent', '2026-04-23T12:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.importer', 'urn:gdst:example.org:location:loc:solution.importerwarehouse', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.importerwarehouse', 'urn:gdst:example.org:party:solution.importer', 'urn:epc:id:sscc:08600031303.solution1', 1);
GO

-- =========================================================
-- TRANSFORMATION EVENTS
-- =========================================================

-- Event 2: gut/bleed wildfish.lot1 into wildfish.lot2 on Vessel #1.
INSERT INTO fulldata.TransformationEvent (EventId, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, ProductOwnerPgln, LocationGln, HumanWelfarePolicy, CountryOfOrigin)
VALUES (2, '2026-04-22T15:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'active', 'urn:gdst:example.org:party:solution.fisherman', 'urn:gdst:example.org:party:solution.fisherman', 'urn:gdst:example.org:location:loc:solution.vessel1', '3p', 'US');

-- Event 9: mill soy.lot1 into feed.lot1 at the Feed Mill.
INSERT INTO fulldata.TransformationEvent (EventId, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, ProductOwnerPgln, LocationGln, CountryOfOrigin, ProteinSource)
VALUES (9, '2026-04-22T22:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'active', 'urn:gdst:example.org:party:solution.feedmill', 'urn:gdst:example.org:party:solution.feedmill', 'urn:gdst:example.org:location:loc:solution.feedmill', 'US', 'soy');

-- Event 15: grow fryling.lot1 + feed.lot1 into farmedfish.lot1 at the Fish Farm.
INSERT INTO fulldata.TransformationEvent (EventId, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, ProductOwnerPgln, LocationGln, HumanWelfarePolicy, CountryOfOrigin, AquacultureMethod, ProductionMethod)
VALUES (15, '2026-04-23T04:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'active', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:party:solution.fishfarm', 'urn:gdst:example.org:location:loc:solution.fishfarm', '3p', 'US', 'intensive ponds', 'AQUACULTURE');

-- Event 18: process wildfish.lot2 into processedwild.final1 at the Processor Plant.
INSERT INTO fulldata.TransformationEvent (EventId, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, ProductOwnerPgln, LocationGln, CountryOfOrigin)
VALUES (18, '2026-04-23T07:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'active', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.processorplant', 'US');

-- Event 19: process farmedfish.lot1 into processedfarmed.final1 at the Processor Plant.
INSERT INTO fulldata.TransformationEvent (EventId, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, ProductOwnerPgln, LocationGln, CountryOfOrigin)
VALUES (19, '2026-04-23T08:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'active', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.processorplant', 'US');
GO

INSERT INTO fulldata.TransformationProduct (EventId, IoType, Epc, Quantity, Uom, ProductGtin) VALUES
(2, 'Input', 'urn:gdst:example.org:product:lot:class:solution.wildfish.lot1', 10000, 'KGM', 'urn:gdst:example.org:product:class:solution.wildfish'),
(2, 'Output', 'urn:gdst:example.org:product:lot:class:solution.wildfish.lot2', 9500, 'KGM', 'urn:gdst:example.org:product:class:solution.wildfish'),
(9, 'Input', 'urn:gdst:example.org:product:lot:class:solution.soy.lot1', 6000, 'KGM', 'urn:gdst:example.org:product:class:solution.soy'),
(9, 'Output', 'urn:gdst:example.org:product:lot:class:solution.feed.lot1', 5000, 'KGM', 'urn:gdst:example.org:product:class:solution.feed'),
(15, 'Input', 'urn:gdst:example.org:product:lot:class:solution.fryling.lot1', 1000, 'KGM', 'urn:gdst:example.org:product:class:solution.fryling'),
(15, 'Input', 'urn:gdst:example.org:product:lot:class:solution.feed.lot1', 5000, 'KGM', 'urn:gdst:example.org:product:class:solution.feed'),
(15, 'Output', 'urn:gdst:example.org:product:lot:class:solution.farmedfish.lot1', 6000, 'KGM', 'urn:gdst:example.org:product:class:solution.farmedfish'),
(18, 'Input', 'urn:gdst:example.org:product:lot:class:solution.wildfish.lot2', 9500, 'KGM', 'urn:gdst:example.org:product:class:solution.wildfish'),
(18, 'Output', 'urn:gdst:example.org:product:lot:class:solution.processedwild.final1', 9000, 'KGM', 'urn:gdst:example.org:product:class:solution.processedwild'),
(19, 'Input', 'urn:gdst:example.org:product:lot:class:solution.farmedfish.lot1', 6000, 'KGM', 'urn:gdst:example.org:product:class:solution.farmedfish'),
(19, 'Output', 'urn:gdst:example.org:product:lot:class:solution.processedfarmed.final1', 5500, 'KGM', 'urn:gdst:example.org:product:class:solution.processedfarmed');
GO

-- =========================================================
-- AGGREGATION EVENTS
-- =========================================================

-- Event 21: pack the processed lots onto the SSCC at the Processor Plant.
INSERT INTO fulldata.AggregationEvent (EventId, EventKind, EventTimeUtc, ReadPoint, InfoProviderPgln, ProductOwnerPgln, LocationGln, ParentEpc)
VALUES (21, 'aggregationevent', '2026-04-23T10:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:party:solution.processor', 'urn:gdst:example.org:location:loc:solution.processorplant', 'urn:epc:id:sscc:08600031303.solution1');

-- Event 24: unpack the SSCC at the Importer Warehouse.
INSERT INTO fulldata.AggregationEvent (EventId, EventKind, EventTimeUtc, ReadPoint, Disposition, InfoProviderPgln, ProductOwnerPgln, LocationGln, ParentEpc)
VALUES (24, 'disaggregationevent', '2026-04-23T13:44:36.6820658+00:00', 'geo:37.7749,-122.4194', 'inactive', 'urn:gdst:example.org:party:solution.importer', 'urn:gdst:example.org:party:solution.importer', 'urn:gdst:example.org:location:loc:solution.importerwarehouse', 'urn:epc:id:sscc:08600031303.solution1');
GO

INSERT INTO fulldata.AggregationChild (EventId, Epc, Quantity, Uom, ProductGtin) VALUES
(21, 'urn:gdst:example.org:product:lot:class:solution.processedwild.final1', 8900, 'KGM', 'urn:gdst:example.org:product:class:solution.processedwild'),
(21, 'urn:gdst:example.org:product:lot:class:solution.processedfarmed.final1', 5500, 'KGM', 'urn:gdst:example.org:product:class:solution.processedfarmed');
GO
