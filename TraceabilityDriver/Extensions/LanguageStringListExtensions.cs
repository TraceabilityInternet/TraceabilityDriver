using OpenTraceability.Models.Common;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for lists of language-tagged strings, such as master data names and descriptions.
    /// </summary>
    public static class LanguageStringListExtensions
    {
        /// <summary>
        /// Merges the source strings into the target list, matching entries by language.
        /// </summary>
        /// <remarks>
        /// Languages missing from the target are added; a language present in both keeps the target's value
        /// and only takes the source's value when the target's is empty. The target must be non-null; a caller
        /// holding a null list has to assign the source itself, since an extension method cannot hand a new
        /// list back.
        /// </remarks>
        /// <param name="targetList">The list to merge into. Modified in place.</param>
        /// <param name="sourceList">The list to merge from. Not modified.</param>
        public static void Merge(this List<LanguageString> targetList, List<LanguageString>? sourceList)
        {
            if (sourceList == null)
            {
                return;
            }

            foreach (LanguageString source in sourceList)
            {
                LanguageString? target = targetList.FirstOrDefault(s => s.Language == source.Language);
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
