using OpenTraceability.GDST.MasterData;
using OpenTraceability.Models.Common;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Models.MasterData;
using OpenTraceability.Utility;
using TraceabilityDriver.Extensions;

namespace TraceabilityDriver.Tests
{
    /// <summary>
    /// Unit tests for <see cref="IVocabularyElementExtensions"/>.
    /// </summary>
    /// <remarks>
    /// The merge exists so master data whose source rows straddle sync runs accumulates into one complete
    /// element. The contract under test is first-wins: the target keeps every value it already has, the source
    /// only fills the gaps, the source object is never modified, and identity properties are never touched.
    /// </remarks>
    [TestFixture]
    [Category("UnitTest")]
    public class IVocabularyElementExtensionTests
    {
        private const string TestGln = "urn:epc:id:sgln:0614141.12345.0";
        private const string TestGtin = "urn:epc:idpat:sgtin:0614141.107346";
        private const string TestPgln = "urn:epc:id:pgln:0614141.12345";

        /// <summary>
        /// A location missing the vessel KDEs must take them from the source location.
        /// </summary>
        [Test]
        public void Merge_LocationMissingProperties_TakesThemFromSource()
        {
            // Arrange
            GDSTLocation target = new GDSTLocation { GLN = new GLN(TestGln), VesselName = "Sea Breeze" };
            GDSTLocation source = new GDSTLocation
            {
                GLN = new GLN(TestGln),
                VesselID = "VID-001",
                VesselFlagState = new Country { Abbreviation = "US", Alpha3 = "USA", ISO = 840 },
                IMONumber = "IMO-9999999",
                UnloadingPort = "USSEA",
                Contact = "Dock Master",
                Email = "dock@example.com",
                Phone = "+1-206-555-0100",
                SatelliteTrackingAuthority = "AuthorityOne",
                VesselPublicRegistry = "RegistryOne"
            };

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.VesselID, Is.EqualTo("VID-001"));
            Assert.That(target.VesselFlagState, Is.Not.Null);
            Assert.That(target.IMONumber, Is.EqualTo("IMO-9999999"));
            Assert.That(target.UnloadingPort, Is.EqualTo("USSEA"));
            Assert.That(target.Contact, Is.EqualTo("Dock Master"));
            Assert.That(target.Email, Is.EqualTo("dock@example.com"));
            Assert.That(target.Phone, Is.EqualTo("+1-206-555-0100"));
            Assert.That(target.SatelliteTrackingAuthority, Is.EqualTo("AuthorityOne"));
            Assert.That(target.VesselPublicRegistry, Is.EqualTo("RegistryOne"));
        }

        /// <summary>
        /// A property set on both locations must keep the target's value, because the target came from the
        /// earlier rows.
        /// </summary>
        [Test]
        public void Merge_LocationPropertySetOnBoth_KeepsTargetValue()
        {
            // Arrange
            GDSTLocation target = new GDSTLocation { GLN = new GLN(TestGln), VesselName = "Sea Breeze", VesselID = "VID-TARGET" };
            GDSTLocation source = new GDSTLocation { GLN = new GLN(TestGln), VesselName = "Other Boat", VesselID = "VID-SOURCE" };

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.VesselName, Is.EqualTo("Sea Breeze"), "Values from earlier rows must win conflicts.");
            Assert.That(target.VesselID, Is.EqualTo("VID-TARGET"));
        }

        /// <summary>
        /// A location with no name must take the source's name list; when both carry names they must be merged
        /// by language rather than replaced.
        /// </summary>
        [Test]
        public void Merge_LocationNames_AssignsWhenMissingAndMergesByLanguage()
        {
            // Arrange
            GDSTLocation emptyTarget = new GDSTLocation { GLN = new GLN(TestGln) };
            GDSTLocation namedSource = new GDSTLocation
            {
                GLN = new GLN(TestGln),
                Name = new List<LanguageString> { new LanguageString { Language = "en", Value = "Seattle Dock" } }
            };

            GDSTLocation partialTarget = new GDSTLocation
            {
                GLN = new GLN(TestGln),
                Name = new List<LanguageString> { new LanguageString { Language = "en", Value = "Seattle Dock" } }
            };
            GDSTLocation otherLanguageSource = new GDSTLocation
            {
                GLN = new GLN(TestGln),
                Name = new List<LanguageString>
                {
                    new LanguageString { Language = "en", Value = "Should Not Win" },
                    new LanguageString { Language = "es", Value = "Muelle de Seattle" }
                }
            };

            // Act
            emptyTarget.Merge(namedSource);
            partialTarget.Merge(otherLanguageSource);

            // Assert
            Assert.That(emptyTarget.Name, Is.Not.Null);
            Assert.That(emptyTarget.Name.Single().Value, Is.EqualTo("Seattle Dock"), "A target with no names must take the source's names.");

            Assert.That(partialTarget.Name, Has.Count.EqualTo(2), "A language missing from the target must be added.");
            Assert.That(partialTarget.Name.First(n => n.Language == "en").Value, Is.EqualTo("Seattle Dock"), "An existing language must keep the target's value.");
            Assert.That(partialTarget.Name.First(n => n.Language == "es").Value, Is.EqualTo("Muelle de Seattle"));
        }

        /// <summary>
        /// The address must be assigned when the target has none, and merged per property when it has one, so
        /// a street from one run and a city from another end up on the same address.
        /// </summary>
        [Test]
        public void Merge_LocationAddress_AssignsWhenMissingAndFillsGapsOtherwise()
        {
            // Arrange
            GDSTLocation emptyTarget = new GDSTLocation { GLN = new GLN(TestGln) };
            GDSTLocation addressedSource = new GDSTLocation
            {
                GLN = new GLN(TestGln),
                Address = new OpenTraceability.Models.MasterData.Address { PostalCode = "98101", Country = new Country { Abbreviation = "US", Alpha3 = "USA", ISO = 840 } }
            };

            GDSTLocation partialTarget = new GDSTLocation
            {
                GLN = new GLN(TestGln),
                Address = new OpenTraceability.Models.MasterData.Address
                {
                    Address1 = new List<LanguageString> { new LanguageString { Language = "en", Value = "1 Pier Street" } }
                }
            };
            GDSTLocation citySource = new GDSTLocation
            {
                GLN = new GLN(TestGln),
                Address = new OpenTraceability.Models.MasterData.Address
                {
                    City = new List<LanguageString> { new LanguageString { Language = "en", Value = "Seattle" } },
                    PostalCode = "98101"
                }
            };

            // Act
            emptyTarget.Merge(addressedSource);
            partialTarget.Merge(citySource);

            // Assert
            Assert.That(emptyTarget.Address, Is.Not.Null);
            Assert.That(emptyTarget.Address.PostalCode, Is.EqualTo("98101"));

            Assert.That(partialTarget.Address.Address1!.Single().Value, Is.EqualTo("1 Pier Street"), "The target's own address lines must survive the merge.");
            Assert.That(partialTarget.Address.City, Is.Not.Null, "A missing address line list must be taken from the source.");
            Assert.That(partialTarget.Address.City!.Single().Value, Is.EqualTo("Seattle"));
            Assert.That(partialTarget.Address.PostalCode, Is.EqualTo("98101"));
        }

        /// <summary>
        /// Location classifications must be merged by type: missing types are added, existing types keep the
        /// target's value.
        /// </summary>
        [Test]
        public void Merge_LocationClassifications_AddsMissingTypesAndKeepsExistingValues()
        {
            // Arrange
            GDSTLocation target = new GDSTLocation { GLN = new GLN(TestGln) };
            target.LocationClassification.Add(new GDSTClassification { Type = "urn:gdst:vocab#locationType", Value = "vessel" });

            GDSTLocation source = new GDSTLocation { GLN = new GLN(TestGln) };
            source.LocationClassification.Add(new GDSTClassification { Type = "urn:gdst:vocab#locationType", Value = "should-not-win" });
            source.LocationClassification.Add(new GDSTClassification { Type = "urn:gdst:vocab#gearType", Value = "purse-seine" });

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.LocationClassification, Has.Count.EqualTo(2));
            Assert.That(target.LocationClassification.First(c => c.Type == "urn:gdst:vocab#locationType").Value, Is.EqualTo("vessel"));
            Assert.That(target.LocationClassification.First(c => c.Type == "urn:gdst:vocab#gearType").Value, Is.EqualTo("purse-seine"));
        }

        /// <summary>
        /// A trade item missing its species and description data must take them from the source, and species
        /// lists must union rather than replace.
        /// </summary>
        [Test]
        public void Merge_TradeItemMissingProperties_TakesThemFromSource()
        {
            // Arrange
            GDSTTradeItem target = new GDSTTradeItem
            {
                GTIN = new GTIN(TestGtin),
                FisherySpeciesCode = new List<string> { "YFT" }
            };
            target.ProductClassification.Add(new GDSTClassification { Type = "urn:gdst:vocab#productForm", Value = "whole" });

            GDSTTradeItem source = new GDSTTradeItem
            {
                GTIN = new GTIN(TestGtin),
                TradeItemConditionCode = "FRESH",
                ShortDescription = new List<LanguageString> { new LanguageString { Language = "en", Value = "Yellowfin Tuna" } },
                FisherySpeciesCode = new List<string> { "YFT", "BET" },
                FisherySpeciesScientificName = new List<string> { "Thunnus albacares" }
            };
            source.ProductClassification.Add(new GDSTClassification { Type = "urn:gdst:vocab#productType", Value = "seafood" });

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.TradeItemConditionCode, Is.EqualTo("FRESH"));
            Assert.That(target.ShortDescription.Single().Value, Is.EqualTo("Yellowfin Tuna"));
            Assert.That(target.FisherySpeciesCode, Is.EqualTo(new List<string> { "YFT", "BET" }), "Species codes must union without duplicating the code the target already had.");
            Assert.That(target.FisherySpeciesScientificName.Single(), Is.EqualTo("Thunnus albacares"));
            Assert.That(target.ProductClassification, Has.Count.EqualTo(2));
        }

        /// <summary>
        /// A trading party missing its name, address, and IFTP must take them from the source.
        /// </summary>
        [Test]
        public void Merge_TradingPartyMissingProperties_TakesThemFromSource()
        {
            // Arrange
            TradingParty target = new TradingParty { PGLN = new PGLN(TestPgln) };
            TradingParty source = new TradingParty
            {
                PGLN = new PGLN(TestPgln),
                IFTP = "processor",
                Name = new List<LanguageString> { new LanguageString { Language = "en", Value = "Acme Seafood" } },
                Address = new OpenTraceability.Models.MasterData.Address { PostalCode = "98101" }
            };

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.IFTP, Is.EqualTo("processor"));
            Assert.That(target.Name.Single().Value, Is.EqualTo("Acme Seafood"));
            Assert.That(target.Address.PostalCode, Is.EqualTo("98101"));
        }

        /// <summary>
        /// A property set on both trading parties must keep the target's value.
        /// </summary>
        [Test]
        public void Merge_TradingPartyPropertySetOnBoth_KeepsTargetValue()
        {
            // Arrange
            TradingParty target = new TradingParty { PGLN = new PGLN(TestPgln), IFTP = "processor" };
            TradingParty source = new TradingParty { PGLN = new PGLN(TestPgln), IFTP = "harvester" };

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.IFTP, Is.EqualTo("processor"));
        }

        /// <summary>
        /// The merge must never modify the source element, because the same source can be merged into more
        /// than one target.
        /// </summary>
        [Test]
        public void Merge_SourceElement_IsNotModified()
        {
            // Arrange
            GDSTLocation target = new GDSTLocation { GLN = new GLN(TestGln), VesselID = "VID-TARGET" };
            GDSTLocation source = new GDSTLocation
            {
                GLN = new GLN(TestGln),
                VesselID = "VID-SOURCE",
                VesselName = "Sea Breeze",
                Name = new List<LanguageString> { new LanguageString { Language = "en", Value = "Seattle Dock" } }
            };

            // Act
            target.Merge(source);

            // Assert
            Assert.That(source.VesselID, Is.EqualTo("VID-SOURCE"));
            Assert.That(source.VesselName, Is.EqualTo("Sea Breeze"));
            Assert.That(source.Name.Single().Value, Is.EqualTo("Seattle Dock"));
        }

        /// <summary>
        /// Identity properties are what the elements were matched on, so the merge must leave them alone.
        /// </summary>
        [Test]
        public void Merge_IdentityProperties_AreNotTouched()
        {
            // Arrange
            GDSTLocation target = new GDSTLocation { GLN = new GLN(TestGln), EPCISType = "urn:epcglobal:epcis:vtype:Location" };
            GDSTLocation source = new GDSTLocation { GLN = new GLN("urn:epc:id:sgln:0614141.12346.0"), EPCISType = "something:else", JsonLDType = "gs1:Other" };

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.GLN.ToString(), Is.EqualTo(TestGln));
            Assert.That(target.EPCISType, Is.EqualTo("urn:epcglobal:epcis:vtype:Location"));
            Assert.That(target.JsonLDType, Is.EqualTo("gs1:Place"));
        }

        /// <summary>
        /// Elements of different vocabulary types have nothing to exchange, so the merge must be a no-op
        /// rather than throw.
        /// </summary>
        [Test]
        public void Merge_MismatchedElementTypes_IsANoOp()
        {
            // Arrange
            GDSTLocation target = new GDSTLocation { GLN = new GLN(TestGln) };
            GDSTTradeItem source = new GDSTTradeItem { GTIN = new GTIN(TestGtin), TradeItemConditionCode = "FRESH" };

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.VesselID, Is.Null);
            Assert.That(target.Name, Is.Null);
            Assert.That(target.LocationClassification, Is.Empty);
        }
    }
}
