using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Extensions
{
    public static class BizTransactionListExtensions
    {
        public static void Merge(this List<EventBusinessTransaction>? targetList, List<EventBusinessTransaction> sourceList)
        {
            if (targetList == null && sourceList != null)
            {
                targetList = new List<EventBusinessTransaction>(sourceList);
            }
            else if (targetList != null && sourceList != null)
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
}
