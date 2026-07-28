using OpenTraceability.GDST.MasterData;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for lists of GDST classifications on locations and trade items.
    /// </summary>
    public static class GDSTClassificationListExtensions
    {
        /// <summary>
        /// Merges the source classifications into the target list, matching entries by classification type.
        /// </summary>
        /// <remarks>
        /// Types missing from the target are added; a type present in both keeps the target's value and only
        /// takes the source's value when the target's is empty. The target must be non-null - the GDST master
        /// data models always initialize these lists - because an extension method cannot hand a new list back
        /// to the caller.
        /// </remarks>
        /// <param name="targetList">The list to merge into. Modified in place.</param>
        /// <param name="sourceList">The list to merge from. Not modified.</param>
        public static void Merge(this List<GDSTClassification> targetList, List<GDSTClassification>? sourceList)
        {
            if (sourceList == null)
            {
                return;
            }

            foreach (GDSTClassification source in sourceList)
            {
                GDSTClassification? target = targetList.FirstOrDefault(c => c.Type == source.Type);
                if (target == null)
                {
                    targetList.Add(source);
                }
                else if (string.IsNullOrEmpty(target.Value) && !string.IsNullOrEmpty(source.Value))
                {
                    target.Value = source.Value;
                }
            }
        }
    }
}
