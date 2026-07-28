using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Extensions
{
    public static class DestinationListExtensions
    {
        public static void Merge(this List<EventDestination>? targetList, List<EventDestination> sourceList)
        {
            if (targetList == null && sourceList != null)
            {
                targetList = new List<EventDestination>(sourceList);
            }
            else if (targetList != null && sourceList != null)
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
}
