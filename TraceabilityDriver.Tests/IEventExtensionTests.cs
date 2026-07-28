using OpenTraceability.GDST.Events;
using OpenTraceability.GDST.Events.KDEs;
using OpenTraceability.Models.Events;
using OpenTraceability.Models.Identifiers;
using OpenTraceability.Utility;
using TraceabilityDriver.Extensions;

namespace TraceabilityDriver.Tests
{
    /// <summary>
    /// Unit tests for <see cref="IEventExtensions"/>.
    /// </summary>
    /// <remarks>
    /// The merge exists so an event whose source rows straddle sync runs accumulates into one complete event.
    /// The contract under test is first-wins: the target keeps every value it already has, the source only
    /// fills the gaps, and the source event is never modified.
    /// </remarks>
    [TestFixture]
    [Category("UnitTest")]
    public class IEventExtensionTests
    {
        private const string FirstEpc = "urn:epc:id:sgtin:0614141.107346.2018";
        private const string SecondEpc = "urn:epc:id:sgtin:0614141.107346.2019";

        /// <summary>
        /// Builds a commissioning event carrying an ILMD, which is the shape the events converter produces.
        /// </summary>
        private static GDSTCommissionEvent CreateCommissionEvent()
        {
            return new GDSTCommissionEvent { ILMD = new GDSTILMD() };
        }

        /// <summary>
        /// Scalar properties the target is missing must be taken from the source, while properties set on both
        /// must keep the target's value.
        /// </summary>
        [Test]
        public void Merge_ScalarProperties_FillsGapsAndKeepsTargetValues()
        {
            // Arrange
            GDSTCommissionEvent target = CreateCommissionEvent();
            target.EventTime = new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.Zero);
            target.HumanWelfarePolicy = "policy-target";

            GDSTCommissionEvent source = CreateCommissionEvent();
            source.EventTime = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
            source.HumanWelfarePolicy = "policy-source";
            source.Action = EventAction.ADD;
            source.CertificationInfo = "cert-info";

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.EventTime, Is.EqualTo(new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.Zero)), "Values from earlier rows must win conflicts.");
            Assert.That(target.HumanWelfarePolicy, Is.EqualTo("policy-target"));
            Assert.That(target.Action, Is.EqualTo(EventAction.ADD), "A property the target lacks must be taken from the source.");
            Assert.That(target.CertificationInfo, Is.EqualTo("cert-info"));
        }

        /// <summary>
        /// A product present only on the source must be added to the target, and a product on both must have
        /// its missing quantity filled without losing the target's own quantity.
        /// </summary>
        [Test]
        public void Merge_Products_AddsMissingProductsAndFillsMissingQuantities()
        {
            // Arrange
            GDSTCommissionEvent target = CreateCommissionEvent();
            target.AddProduct(new EventProduct(new EPC(FirstEpc)) { Type = EventProductType.Reference });

            GDSTCommissionEvent source = CreateCommissionEvent();
            source.AddProduct(new EventProduct(new EPC(FirstEpc)) { Type = EventProductType.Reference, Quantity = new Measurement(100.5, "KGM") });
            source.AddProduct(new EventProduct(new EPC(SecondEpc)) { Type = EventProductType.Reference, Quantity = new Measurement(42, "KGM") });

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.Products, Has.Count.EqualTo(2), "A product the target does not carry must be added.");

            EventProduct firstProduct = target.Products.First(p => p.EPC.ToString() == FirstEpc);
            Assert.That(firstProduct.Quantity, Is.Not.Null, "A product whose quantity only arrived in a later row must pick it up.");
            Assert.That(firstProduct.Quantity!.Value, Is.EqualTo(100.5));

            EventProduct secondProduct = target.Products.First(p => p.EPC.ToString() == SecondEpc);
            Assert.That(secondProduct.Quantity!.Value, Is.EqualTo(42));
        }

        /// <summary>
        /// A product carrying a quantity in both events must keep the target's quantity.
        /// </summary>
        [Test]
        public void Merge_ProductQuantitySetOnBoth_KeepsTargetQuantity()
        {
            // Arrange
            GDSTCommissionEvent target = CreateCommissionEvent();
            target.AddProduct(new EventProduct(new EPC(FirstEpc)) { Type = EventProductType.Reference, Quantity = new Measurement(10, "KGM") });

            GDSTCommissionEvent source = CreateCommissionEvent();
            source.AddProduct(new EventProduct(new EPC(FirstEpc)) { Type = EventProductType.Reference, Quantity = new Measurement(999, "LBR") });

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.Products.Single().Quantity!.Value, Is.EqualTo(10));
        }

        /// <summary>
        /// An ILMD with no vessel catch information must take the source's list, since a list cannot be created
        /// from inside the list's own merge.
        /// </summary>
        [Test]
        public void Merge_ILMDWithNoVesselCatchInformation_TakesTheSourceList()
        {
            // Arrange
            GDSTCommissionEvent target = CreateCommissionEvent();
            target.ILMD.BroodstockSource = "hatchery-target";

            GDSTCommissionEvent source = CreateCommissionEvent();
            source.ILMD.VesselCatchInformationList = new VesselCatchInformationList();
            source.ILMD.VesselCatchInformationList.Vessels.Add(new VesselCatchInformation { VesselID = "VID-001", VesselName = "Sea Breeze" });
            source.ILMD.AquacultureMethod = "pond";

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.ILMD.VesselCatchInformationList, Is.Not.Null);
            Assert.That(target.ILMD.VesselCatchInformationList!.Vessels.Single().VesselID, Is.EqualTo("VID-001"));
            Assert.That(target.ILMD.BroodstockSource, Is.EqualTo("hatchery-target"));
            Assert.That(target.ILMD.AquacultureMethod, Is.EqualTo("pond"));
        }

        /// <summary>
        /// An ILMD holding an empty vessel list must be filled from the source rather than left empty.
        /// </summary>
        [Test]
        public void Merge_ILMDWithEmptyVesselList_TakesTheSourceVessels()
        {
            // Arrange
            GDSTCommissionEvent target = CreateCommissionEvent();
            target.ILMD.VesselCatchInformationList = new VesselCatchInformationList();

            GDSTCommissionEvent source = CreateCommissionEvent();
            source.ILMD.VesselCatchInformationList = new VesselCatchInformationList();
            source.ILMD.VesselCatchInformationList.Vessels.Add(new VesselCatchInformation { VesselID = "VID-001" });

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.ILMD.VesselCatchInformationList!.Vessels.Single().VesselID, Is.EqualTo("VID-001"));
        }

        /// <summary>
        /// Two partially populated vessels must merge into one, with the target's values winning conflicts.
        /// </summary>
        [Test]
        public void Merge_VesselInformationSplitAcrossEvents_CombinesIntoOneVessel()
        {
            // Arrange
            GDSTCommissionEvent target = CreateCommissionEvent();
            target.ILMD.VesselCatchInformationList = new VesselCatchInformationList();
            target.ILMD.VesselCatchInformationList.Vessels.Add(new VesselCatchInformation { VesselName = "Sea Breeze", CatchArea = "67" });

            GDSTCommissionEvent source = CreateCommissionEvent();
            source.ILMD.VesselCatchInformationList = new VesselCatchInformationList();
            source.ILMD.VesselCatchInformationList.Vessels.Add(new VesselCatchInformation
            {
                VesselName = "Other Boat",
                VesselID = "VID-001",
                GearType = "purse-seine",
                VesselFlagState = new Country { Abbreviation = "US", Alpha3 = "USA", ISO = 840 }
            });

            // Act
            target.Merge(source);

            // Assert
            VesselCatchInformation vessel = target.ILMD.VesselCatchInformationList!.Vessels.Single();
            Assert.That(vessel.VesselName, Is.EqualTo("Sea Breeze"), "Values from earlier rows must win conflicts.");
            Assert.That(vessel.CatchArea, Is.EqualTo("67"));
            Assert.That(vessel.VesselID, Is.EqualTo("VID-001"));
            Assert.That(vessel.GearType, Is.EqualTo("purse-seine"));
            Assert.That(vessel.VesselFlagState, Is.Not.Null);
        }

        /// <summary>
        /// Source and destination entries missing from the target must be added, and entries of a type present
        /// on both must keep the target's value.
        /// </summary>
        [Test]
        public void Merge_SourceAndDestinationLists_AddMissingTypesAndKeepTargetValues()
        {
            // Arrange
            GDSTCommissionEvent target = CreateCommissionEvent();
            target.SourceList.Add(new EventSource { Type = OpenTraceability.Constants.EPCIS.URN.SDT_Owner, Value = "urn:epc:id:pgln:0614141.12345" });

            GDSTCommissionEvent source = CreateCommissionEvent();
            source.SourceList.Add(new EventSource { Type = OpenTraceability.Constants.EPCIS.URN.SDT_Owner, Value = "urn:epc:id:pgln:0614141.12349" });
            source.SourceList.Add(new EventSource { Type = OpenTraceability.Constants.EPCIS.URN.SDT_Location, Value = "urn:epc:id:sgln:0614141.12345.0" });
            source.DestinationList.Add(new EventDestination { Type = OpenTraceability.Constants.EPCIS.URN.SDT_Owner, Value = "urn:epc:id:pgln:0614141.12346" });

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.SourceList, Has.Count.EqualTo(2));
            Assert.That(target.SourceList.First(s => s.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Owner).Value, Is.EqualTo("urn:epc:id:pgln:0614141.12345"));
            Assert.That(target.SourceList.Any(s => s.Type == OpenTraceability.Constants.EPCIS.URN.SDT_Location), Is.True);
            Assert.That(target.DestinationList, Has.Count.EqualTo(1), "A destination the target does not carry must be added.");
        }

        /// <summary>
        /// The merge must never modify the source event, because the same source can be merged into more than
        /// one target.
        /// </summary>
        [Test]
        public void Merge_SourceEvent_IsNotModified()
        {
            // Arrange
            GDSTCommissionEvent target = CreateCommissionEvent();
            target.HumanWelfarePolicy = "policy-target";

            GDSTCommissionEvent source = CreateCommissionEvent();
            source.HumanWelfarePolicy = "policy-source";
            source.EventTime = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
            source.AddProduct(new EventProduct(new EPC(FirstEpc)) { Type = EventProductType.Reference, Quantity = new Measurement(42, "KGM") });

            // Act
            target.Merge(source);

            // Assert
            Assert.That(source.HumanWelfarePolicy, Is.EqualTo("policy-source"));
            Assert.That(source.EventTime, Is.EqualTo(new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero)));
            Assert.That(source.Products, Has.Count.EqualTo(1));
        }
    }
}
