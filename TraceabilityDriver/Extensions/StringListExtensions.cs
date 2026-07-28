namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for plain string lists on master data, such as the fishery species lists.
    /// </summary>
    public static class StringListExtensions
    {
        /// <summary>
        /// Adds the source entries that the target does not already contain.
        /// </summary>
        /// <remarks>
        /// These lists carry unordered sets of codes and names, so the merge is a union rather than a
        /// positional merge. The target must be non-null; a caller holding a null list has to assign the
        /// source itself, since an extension method cannot hand a new list back.
        /// </remarks>
        /// <param name="targetList">The list to merge into. Modified in place.</param>
        /// <param name="sourceList">The list to merge from. Not modified.</param>
        public static void Merge(this List<string> targetList, List<string>? sourceList)
        {
            if (sourceList == null)
            {
                return;
            }

            foreach (string source in sourceList)
            {
                if (!targetList.Contains(source))
                {
                    targetList.Add(source);
                }
            }
        }
    }
}
