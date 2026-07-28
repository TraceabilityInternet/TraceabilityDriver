using TraceabilityDriver.Models.GDST;

namespace TraceabilityDriver.Services.GDST
{
    /// <summary>
    /// Runs the GDST capability test against the data already held in the driver's traceability cache.
    /// </summary>
    /// <remarks>
    /// The driver does not seed any example data for the test. The caller supplies the top-of-chain EPCs
    /// of its own synced data, and the capability tool traces those EPCs back through the driver — so a
    /// passing result reflects what the configured mapping files actually produce. A successful sync must
    /// therefore have completed before a test is started.
    /// </remarks>
    public interface IGDSTCapabilityTestService
    {
        /// <summary>
        /// Runs a capability test against the supplied top-of-chain EPCs and returns the tool's report.
        /// </summary>
        /// <param name="solutionProviderEPCs">
        /// The top-of-chain EPCs of the already-synced data the capability tool should trace back.
        /// At least one is required, and every entry must be a valid EPC.
        /// </param>
        /// <returns>
        /// The report from the capability tool. Failures that occur once the run has started (the tool
        /// rejecting the request, no data being traceable, the report timing out) are returned as a
        /// <see cref="GDSTCapabilityTestStatus.Failed"/> result rather than thrown.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="solutionProviderEPCs"/> contains no usable entries, or when any
        /// entry is not a valid EPC. Argument problems throw so they are not mistaken for test failures.
        /// </exception>
        Task<GDSTCapabilityTestResults> RunTestAsync(List<string> solutionProviderEPCs);
    }
}
