using Microsoft.Extensions.Configuration;
using OpenTraceability.Interfaces;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;
using OpenTraceability.Queries;
using TraceabilityDriver.Services;

namespace TraceabilityDriver.Tests.Services
{
    /// <summary>
    /// Shared event query filter tests that must hold for every <see cref="IDatabaseService"/> backend.
    /// </summary>
    /// <remarks>
    /// Both backends must translate every supported EPCIS query parameter into an effective database
    /// filter with the same semantics as the in-memory <c>EPCISBaseDocument.FilterEvents</c> reference:
    /// time bounds compare UTC instants regardless of the offsets events were reported with (GE_
    /// inclusive, LT_ exclusive), record time is always the moment an event was stored into the data
    /// cache, MATCH_anyEPC/MATCH_anyEPCClass match the EPCs of all products while MATCH_epc and
    /// MATCH_epcClass match only reference and child products, every MATCH_* value accepts a trailing
    /// * wildcard, values match case-insensitively, EQ_bizStep accepts short CBV names, CBV urns, and
    /// GS1 web vocabulary URIs, and EQ_transformationID only matches transformation events. The test
    /// events come from Data/queryfilter_testdata.json, whose UTC offsets are chosen so that local
    /// clock ordering contradicts UTC ordering, which catches backends that compare local times.
    /// Concrete fixtures supply the backend.
    /// </remarks>
    public abstract class DatabaseServiceQueryTestsBase
    {
        /// <summary>
        /// The deployment version configured in appsettings.Tests.json, which the backends under test
        /// read for their query paths.
        /// </summary>
        protected const string TestDeploymentVersion = "tests";

        private const string WildLot1Epc = "urn:gdst:example.org:product:lot:class:qftest.wild.lot1";
        private const string FarmLot1Epc = "urn:gdst:example.org:product:lot:class:qftest.farm.lot1";
        private const string ParentSsccEpc = "urn:epc:id:sscc:08600031303.qftest1";
        private const string WildLotWildcard = "urn:gdst:example.org:product:lot:class:qftest.wild.*";
        private const string TransformationId = "urn:gdst:example.org:transformation:qftest.transform1";
        private const string PortLocation = "urn:gdst:example.org:location:loc:qftest.port";

        /// <summary>
        /// The instant shared by the shipping event (08:00-05:00) and used as the time range boundary.
        /// </summary>
        private static readonly DateTimeOffset BoundaryInstant = DateTimeOffset.Parse("2026-05-01T13:00:00+00:00");

        protected IDatabaseService _dbService = null!;
        protected bool _skipTests = false;
        private EPCISDocument _testDocument = null!;
        private List<Uri> _eventKeys = null!;

        /// <summary>
        /// The environment variable that, when TRUE, skips this backend's tests.
        /// </summary>
        protected abstract string SkipEnvironmentVariable { get; }

        /// <summary>
        /// Creates the backend under test from the test configuration.
        /// </summary>
        protected abstract IDatabaseService CreateService(IConfiguration configuration);

        /// <summary>
        /// Builds the backend once, clears it, and stores the query filter test events. The stored
        /// event objects keep their identity, so after the store each accessor's EventID carries the
        /// generated content-hash id the backend returns from queries.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            string skipValue = Environment.GetEnvironmentVariable(SkipEnvironmentVariable) ?? string.Empty;
            _skipTests = skipValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

            if (_skipTests)
            {
                Assert.Ignore($"Tests skipped due to {SkipEnvironmentVariable} environment variable set to TRUE");
                return;
            }

            OpenTraceability.GDST.Setup.Initialize();

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Tests.json")
                .Build();

            _dbService = CreateService(configuration);

            await _dbService.ClearDatabaseAsync();
            await _dbService.InitializeDatabase();

            string testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "queryfilter_testdata.json");
            _testDocument = OpenTraceabilityMappers.EPCISDocument.JSON.Map(File.ReadAllText(testDataPath));

            // The incoming EventID acts as the event key and is replaced by the store with the
            // content-hash event id; the keys are kept so tests can re-store events the way a resync
            // would.
            _eventKeys = _testDocument.Events.Select(e => e.EventID).ToList();

            await _dbService.StoreEventsAsync(_testDocument.Events, TestDeploymentVersion);
        }

        /// <summary>
        /// The object event at 10:00Z: commissioning, ADD, vessel1, reference EPC wild.lot1.
        /// </summary>
        private IEvent CommissioningEvent => _testDocument.Events[0];

        /// <summary>
        /// The object event at 13:00Z (08:00-05:00): shipping, OBSERVE, vessel2, reference EPC wild.lot2.
        /// </summary>
        private IEvent ShippingEvent => _testDocument.Events[1];

        /// <summary>
        /// The transformation event at 14:00Z (16:00+02:00): commissioning, plant, input EPC wild.lot1,
        /// output EPC farm.lot1, transformation id transform1.
        /// </summary>
        private IEvent TransformEvent => _testDocument.Events[2];

        /// <summary>
        /// The aggregation event at 15:00Z (08:00-07:00): packing, ADD, port, parent SSCC, child EPC farm.lot1.
        /// </summary>
        private IEvent PackingEvent => _testDocument.Events[3];

        /// <summary>
        /// The object event at 16:00Z: destroying, DELETE, port, reference EPC farm.lot2.
        /// </summary>
        private IEvent DestroyingEvent => _testDocument.Events[4];

        /// <summary>
        /// Skips the current test when the backend is unavailable.
        /// </summary>
        protected void SkipIfUnavailable()
        {
            if (_skipTests)
            {
                Assert.Ignore($"Test skipped due to {SkipEnvironmentVariable} environment variable set to TRUE");
            }
        }

        /// <summary>
        /// Runs the query against the backend and returns the sorted event ids of the results.
        /// </summary>
        private async Task<List<string>> QueryEventIdsAsync(EPCISQuery query)
        {
            EPCISQueryDocument result = await _dbService.QueryEvents(new EPCISQueryParameters { query = query });
            return result.Events.Select(e => e.EventID!.ToString()).OrderBy(id => id).ToList();
        }

        /// <summary>
        /// The sorted stored event ids of the given events, for comparing against query results.
        /// </summary>
        private static List<string> ExpectedIds(params IEvent[] events)
        {
            return events.Select(e => e.EventID.ToString()).OrderBy(id => id).ToList();
        }

        // --- Event time: GE_ inclusive, LT_ exclusive, compared as UTC instants ---

        /// <summary>
        /// GE_eventTime must include the event at the boundary instant and compare UTC instants: the
        /// shipping and packing events have the earliest local clock times but late UTC times.
        /// </summary>
        [Test]
        public async Task QueryEvents_GEEventTime_IncludesBoundaryAndComparesUtcInstants()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { GE_eventTime = BoundaryInstant });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(ShippingEvent, TransformEvent, PackingEvent, DestroyingEvent)), "GE_eventTime must be inclusive and must order events by UTC instant, not local clock time.");
        }

        /// <summary>
        /// LT_eventTime must exclude the event at the boundary instant.
        /// </summary>
        [Test]
        public async Task QueryEvents_LTEventTime_ExcludesBoundary()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { LT_eventTime = BoundaryInstant });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent)), "LT_eventTime must be exclusive of the bound and must compare UTC instants.");
        }

        /// <summary>
        /// A combined event time window must return exactly the events inside it.
        /// </summary>
        [Test]
        public async Task QueryEvents_EventTimeRange_ReturnsEventsInsideWindow()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery
            {
                GE_eventTime = DateTimeOffset.Parse("2026-05-01T10:00:00+00:00"),
                LT_eventTime = DateTimeOffset.Parse("2026-05-01T15:00:00+00:00")
            });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent, ShippingEvent, TransformEvent)));
        }

        /// <summary>
        /// A bound expressed with a non-UTC offset must behave exactly like the same instant in UTC.
        /// </summary>
        [Test]
        public async Task QueryEvents_GEEventTimeWithNonUtcOffset_MatchesSameInstant()
        {
            SkipIfUnavailable();

            // Act - the bound is the boundary instant expressed as 08:00-05:00.
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { GE_eventTime = BoundaryInstant.ToOffset(TimeSpan.FromHours(-5)) });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(ShippingEvent, TransformEvent, PackingEvent, DestroyingEvent)), "The offset a bound is expressed in must not change which events match.");
        }

        // --- Record time: always the store moment, GE_ inclusive, LT_ exclusive ---

        /// <summary>
        /// Record time bounds must split events by the moment they were stored, the stamp must
        /// overwrite any record time the event arrived with, and the returned events must carry the
        /// stamped value. Guards the regression where the record time filter threw
        /// "Expression not supported: Convert(e.RecordTime, DateTimeOffset)" on the Mongo backend.
        /// </summary>
        [Test]
        public async Task QueryEvents_RecordTimeBounds_SplitEventsByStoreMoment()
        {
            SkipIfUnavailable();

            // Arrange - capture a cut after the fixture's store, then re-store the packing and
            // destroying events the way a resync would: event keys restored and a stale record time
            // stamped on the incoming copy that the store must overwrite.
            await Task.Delay(250);
            DateTimeOffset cut = DateTimeOffset.UtcNow;
            await Task.Delay(250);

            PackingEvent.EventID = _eventKeys[3];
            DestroyingEvent.EventID = _eventKeys[4];
            PackingEvent.RecordTime = DateTimeOffset.UtcNow.AddYears(-1);
            DestroyingEvent.RecordTime = DateTimeOffset.UtcNow.AddYears(-1);
            await _dbService.StoreEventsAsync(new List<IEvent> { PackingEvent, DestroyingEvent }, TestDeploymentVersion);

            // Act
            List<string> afterCut = await QueryEventIdsAsync(new EPCISQuery { GE_recordTime = cut });
            List<string> beforeCut = await QueryEventIdsAsync(new EPCISQuery { LT_recordTime = cut });
            EPCISQueryDocument afterCutDocument = await _dbService.QueryEvents(new EPCISQueryParameters { query = new EPCISQuery { GE_recordTime = cut } });

            // Assert
            Assert.That(afterCut, Is.EqualTo(ExpectedIds(PackingEvent, DestroyingEvent)), "GE_recordTime must return only the events stored after the cut.");
            Assert.That(beforeCut, Is.EqualTo(ExpectedIds(CommissioningEvent, ShippingEvent, TransformEvent)), "LT_recordTime must return only the events stored before the cut.");
            Assert.That(afterCutDocument.Events.All(e => e.RecordTime >= cut), Is.True, "The returned events must carry the store-time record time, not the stale value they arrived with.");
        }

        // --- MATCH_anyEPC / MATCH_epc: any product type versus reference and child only ---

        /// <summary>
        /// MATCH_anyEPC must match the EPCs of every product type, including transformation inputs.
        /// </summary>
        [Test]
        public async Task QueryEvents_MatchAnyEpc_MatchesReferenceAndInputEpcs()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { MATCH_anyEPC = new List<string> { WildLot1Epc } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent, TransformEvent)), "MATCH_anyEPC must match both the reference EPC and the transformation input EPC.");
        }

        /// <summary>
        /// MATCH_anyEPC must match the parent EPC of an aggregation event.
        /// </summary>
        [Test]
        public async Task QueryEvents_MatchAnyEpc_MatchesParentEpc()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { MATCH_anyEPC = new List<string> { ParentSsccEpc } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(PackingEvent)));
        }

        /// <summary>
        /// MATCH_anyEPC values must match case-insensitively.
        /// </summary>
        [Test]
        public async Task QueryEvents_MatchAnyEpcUppercaseValue_MatchesCaseInsensitively()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { MATCH_anyEPC = new List<string> { WildLot1Epc.ToUpper() } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent, TransformEvent)));
        }

        /// <summary>
        /// MATCH_epc must not match transformation input EPCs; only reference and child products count.
        /// </summary>
        [Test]
        public async Task QueryEvents_MatchEpc_ExcludesInputEpcs()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { MATCH_epc = new List<string> { WildLot1Epc } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent)), "MATCH_epc must exclude the transformation event whose only use of the EPC is as an input.");
        }

        /// <summary>
        /// MATCH_epc must not match the parent EPC of an aggregation event.
        /// </summary>
        [Test]
        public async Task QueryEvents_MatchEpc_ExcludesParentEpcs()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { MATCH_epc = new List<string> { ParentSsccEpc } });

            // Assert
            Assert.That(results, Is.Empty, "MATCH_epc must exclude parent EPCs; only MATCH_anyEPC matches them.");
        }

        /// <summary>
        /// MATCH_epcClass must match child EPCs but not transformation output EPCs.
        /// </summary>
        [Test]
        public async Task QueryEvents_MatchEpcClass_MatchesChildButNotOutputEpcs()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { MATCH_epcClass = new List<string> { FarmLot1Epc } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(PackingEvent)), "MATCH_epcClass must match the child EPC of the packing event but not the output EPC of the transformation event.");
        }

        /// <summary>
        /// MATCH_anyEPCClass must match both transformation output EPCs and child EPCs.
        /// </summary>
        [Test]
        public async Task QueryEvents_MatchAnyEpcClass_MatchesOutputAndChildEpcs()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { MATCH_anyEPCClass = new List<string> { FarmLot1Epc } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(TransformEvent, PackingEvent)));
        }

        /// <summary>
        /// A trailing * wildcard on MATCH_anyEPCClass must match every EPC with the prefix, across all product types.
        /// </summary>
        [Test]
        public async Task QueryEvents_MatchAnyEpcClassWildcard_MatchesByPrefix()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { MATCH_anyEPCClass = new List<string> { WildLotWildcard } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent, ShippingEvent, TransformEvent)), "The wildcard must match wild.lot1 and wild.lot2 on every product type, including the transformation input.");
        }

        /// <summary>
        /// A trailing * wildcard on MATCH_epcClass must match by prefix while still excluding input EPCs.
        /// </summary>
        [Test]
        public async Task QueryEvents_MatchEpcClassWildcard_MatchesByPrefixOnReferenceAndChildOnly()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { MATCH_epcClass = new List<string> { WildLotWildcard } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent, ShippingEvent)), "The wildcard must respect the reference/child restriction of MATCH_epcClass.");
        }

        // --- eventTypes ---

        /// <summary>
        /// The eventTypes parameter must filter events by their EPCIS event type.
        /// </summary>
        [Test]
        public async Task QueryEvents_EventTypesObjectEvent_ReturnsOnlyObjectEvents()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { eventTypes = new List<string> { "ObjectEvent" } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent, ShippingEvent, DestroyingEvent)));
        }

        /// <summary>
        /// Multiple eventTypes values must be combined with OR semantics.
        /// </summary>
        [Test]
        public async Task QueryEvents_EventTypesMultipleValues_ReturnsUnionOfTypes()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { eventTypes = new List<string> { "TransformationEvent", "AggregationEvent" } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(TransformEvent, PackingEvent)));
        }

        // --- EQ_bizStep: short CBV name, CBV urn, and GS1 web vocabulary forms ---

        /// <summary>
        /// EQ_bizStep must accept the short CBV name form.
        /// </summary>
        [Test]
        public async Task QueryEvents_EQBizStepShortName_MatchesStoredEvents()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { EQ_bizStep = new List<string> { "commissioning" } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent, TransformEvent)));
        }

        /// <summary>
        /// EQ_bizStep must accept the CBV urn form.
        /// </summary>
        [Test]
        public async Task QueryEvents_EQBizStepUrn_MatchesStoredEvents()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { EQ_bizStep = new List<string> { "urn:epcglobal:cbv:bizstep:shipping" } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(ShippingEvent)));
        }

        /// <summary>
        /// EQ_bizStep must accept the GS1 web vocabulary URI form.
        /// </summary>
        [Test]
        public async Task QueryEvents_EQBizStepWebVocab_MatchesStoredEvents()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { EQ_bizStep = new List<string> { "https://ref.gs1.org/cbv/BizStep-packing" } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(PackingEvent)));
        }

        // --- EQ_action ---

        /// <summary>
        /// EQ_action must match events by action with OR semantics across values.
        /// </summary>
        [Test]
        public async Task QueryEvents_EQActionAdd_ReturnsAddEvents()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { EQ_action = new List<string> { "ADD" } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent, PackingEvent)));
        }

        /// <summary>
        /// EQ_action values must match case-insensitively.
        /// </summary>
        [Test]
        public async Task QueryEvents_EQActionLowercaseValue_MatchesCaseInsensitively()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { EQ_action = new List<string> { "observe" } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(ShippingEvent)));
        }

        // --- EQ_bizLocation ---

        /// <summary>
        /// EQ_bizLocation must return every event at the given business location.
        /// </summary>
        [Test]
        public async Task QueryEvents_EQBizLocation_ReturnsEventsAtLocation()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { EQ_bizLocation = new List<Uri> { new Uri(PortLocation) } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(PackingEvent, DestroyingEvent)));
        }

        // --- EQ_transformationID ---

        /// <summary>
        /// EQ_transformationID must return only the transformation event carrying the id.
        /// </summary>
        [Test]
        public async Task QueryEvents_EQTransformationID_ReturnsOnlyMatchingTransformationEvent()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { EQ_transformationID = new List<string> { TransformationId } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(TransformEvent)), "Only transformation events can match EQ_transformationID; every other event must be excluded.");
        }

        /// <summary>
        /// EQ_transformationID values must match case-insensitively.
        /// </summary>
        [Test]
        public async Task QueryEvents_EQTransformationIDUppercaseValue_MatchesCaseInsensitively()
        {
            SkipIfUnavailable();

            // Act
            List<string> results = await QueryEventIdsAsync(new EPCISQuery { EQ_transformationID = new List<string> { TransformationId.ToUpper() } });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(TransformEvent)));
        }

        // --- Combined parameters ---

        /// <summary>
        /// Multiple different parameters must combine with AND semantics.
        /// </summary>
        [Test]
        public async Task QueryEvents_CombinedParameters_AndSemantics()
        {
            SkipIfUnavailable();

            // Act - object events with action ADD from 09:00Z onward; only the commissioning event
            // satisfies all three parameters.
            List<string> results = await QueryEventIdsAsync(new EPCISQuery
            {
                eventTypes = new List<string> { "ObjectEvent" },
                EQ_action = new List<string> { "ADD" },
                GE_eventTime = DateTimeOffset.Parse("2026-05-01T09:00:00+00:00")
            });

            // Assert
            Assert.That(results, Is.EqualTo(ExpectedIds(CommissioningEvent)));
        }
    }
}
