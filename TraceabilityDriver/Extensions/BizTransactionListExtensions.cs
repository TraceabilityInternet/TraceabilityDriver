using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for an event's business transaction list.
    /// </summary>
    public static class BizTransactionListExtensions
    {
        /// <summary>
        /// Merges the source business transactions into the target list, matching entries by transaction type.
        /// </summary>
        /// <remarks>
        /// Types missing from the target are added; a type present in both keeps the target's value and only
        /// takes the source's value when the target's is empty. Both lists must be non-null - the event
        /// models always initialize them - because an extension method cannot hand a new list back to the
        /// caller.
        /// </remarks>
        /// <param name="targetList">The list to merge into. Modified in place.</param>
        /// <param name="sourceList">The list to merge from. Not modified.</param>
        public static void Merge(this List<EventBusinessTransaction> targetList, List<EventBusinessTransaction> sourceList)
        {
            foreach (var sourceBizTransaction in sourceList)
            {
                var targetBizTransaction = targetList.FirstOrDefault(bt => bt.Type == sourceBizTransaction.Type);
                if (targetBizTransaction == null)
                {
                    targetList.Add(sourceBizTransaction);
                }
                else
                {
                    if (string.IsNullOrEmpty(targetBizTransaction.Value) && !string.IsNullOrEmpty(sourceBizTransaction.Value))
                    {
                        targetBizTransaction.Value = sourceBizTransaction.Value;
                    }
                }
            }
        }
    }
}
