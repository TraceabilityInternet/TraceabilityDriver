using System;
using System.Collections.Generic;

namespace Extensions
{
    public static class IEnumerableExtensions
    {
        public static List<List<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

            var result = new List<List<T>>();
            var batch = new List<T>(batchSize);

            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    result.Add(new List<T>(batch));
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                result.Add(batch);
            }

            return result;
        }
    }
}
