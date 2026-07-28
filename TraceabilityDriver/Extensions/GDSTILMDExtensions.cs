using OpenTraceability.GDST.Events;

namespace TraceabilityDriver.Extensions
{
    public static class GDSTILMDExtensions
    {
        public static void Merge(this GDSTILMD? target, GDSTILMD source)
        {
            if (target == null && source != null)
            {
                target = source;
            }
            else if (target != null && source != null)
            {
                target.VesselCatchInformationList.Merge(source.VesselCatchInformationList);

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
}
