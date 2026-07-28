using OpenTraceability.GDST.Events;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for the GDST instance/lot master data of an event.
    /// </summary>
    public static class GDSTILMDExtensions
    {
        /// <summary>
        /// Fills the target ILMD's unset properties from the source ILMD.
        /// </summary>
        /// <remarks>
        /// The target must be non-null. <c>IGDSTILMDEvent.ILMD</c> is read-only on the interface, so a caller
        /// merging through it cannot create a missing ILMD; the events converter always assigns one.
        /// </remarks>
        /// <param name="target">The ILMD to merge into. Modified in place.</param>
        /// <param name="source">The ILMD to merge from. Not modified.</param>
        public static void Merge(this GDSTILMD target, GDSTILMD? source)
        {
            if (source == null)
            {
                return;
            }

            // The vessel catch information list is a reference the caller cannot replace from inside
            // VesselCatchInformationListExtensions.Merge, so a missing list is assigned here.
            if (target.VesselCatchInformationList == null)
            {
                target.VesselCatchInformationList = source.VesselCatchInformationList;
            }
            else
            {
                target.VesselCatchInformationList.Merge(source.VesselCatchInformationList);
            }

            if (string.IsNullOrEmpty(target.BroodstockSource) && !string.IsNullOrEmpty(source.BroodstockSource))
            {
                target.BroodstockSource = source.BroodstockSource;
            }

            if (string.IsNullOrEmpty(target.AquacultureMethod) && !string.IsNullOrEmpty(source.AquacultureMethod))
            {
                target.AquacultureMethod = source.AquacultureMethod;
            }

            if (string.IsNullOrEmpty(target.ProteinSource) && !string.IsNullOrEmpty(source.ProteinSource))
            {
                target.ProteinSource = source.ProteinSource;
            }
        }
    }
}
