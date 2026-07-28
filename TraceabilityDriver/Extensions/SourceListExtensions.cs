using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for an event's source list.
    /// </summary>
    public static class SourceListExtensions
    {
        /// <summary>
        /// Merges the source entries into the target list, matching entries by source type.
        /// </summary>
        /// <remarks>
        /// Types missing from the target are added; a type present in both keeps the target's value and only
        /// takes the source's value when the target's is empty. Both lists must be non-null - the event
        /// models always initialize them - because an extension method cannot hand a new list back to the
        /// caller.
        /// </remarks>
        /// <param name="targetList">The list to merge into. Modified in place.</param>
        /// <param name="sourceList">The list to merge from. Not modified.</param>
        public static void Merge(this List<EventSource> targetList, List<EventSource> sourceList)
        {
            foreach (var source in sourceList)
            {
                var target = targetList.FirstOrDefault(s => s.Type == source.Type);
                if (target == null)
                {
                    targetList.Add(source);
                }
                else
                {
                    if (string.IsNullOrEmpty(target.Value) && !string.IsNullOrEmpty(source.Value))
                    {
                        target.Value = source.Value;
                    }
                }
            }
        }
    }
}
