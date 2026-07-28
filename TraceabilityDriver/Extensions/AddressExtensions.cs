using OpenTraceability.Models.MasterData;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for a master data postal address.
    /// </summary>
    public static class AddressExtensions
    {
        /// <summary>
        /// Fills the target address's unset properties from the source address.
        /// </summary>
        /// <remarks>
        /// The target must be non-null; a caller holding a null address has to assign the source itself, since
        /// an extension method cannot hand a new instance back. The language-tagged line properties are merged
        /// per language rather than replaced wholesale, so an address whose street came from one sync run and
        /// whose city came from another ends up with both.
        /// </remarks>
        /// <param name="target">The address to merge into. Modified in place.</param>
        /// <param name="source">The address to merge from. Not modified.</param>
        public static void Merge(this Address target, Address? source)
        {
            if (source == null)
            {
                return;
            }

            target.Address1 = MergeLines(target.Address1, source.Address1);
            target.Address2 = MergeLines(target.Address2, source.Address2);
            target.City = MergeLines(target.City, source.City);
            target.State = MergeLines(target.State, source.State);

            if (string.IsNullOrEmpty(target.PostalCode) && !string.IsNullOrEmpty(source.PostalCode))
            {
                target.PostalCode = source.PostalCode;
            }

            if (target.Country == null && source.Country != null)
            {
                target.Country = source.Country;
            }

            if (target.AddressCountry == null && source.AddressCountry != null)
            {
                target.AddressCountry = source.AddressCountry;
            }
        }

        /// <summary>
        /// Merges one language-tagged address line list into another, assigning the source list when the
        /// target has none.
        /// </summary>
        private static List<OpenTraceability.Models.Common.LanguageString>? MergeLines(List<OpenTraceability.Models.Common.LanguageString>? target, List<OpenTraceability.Models.Common.LanguageString>? source)
        {
            if (target == null)
            {
                return source;
            }

            target.Merge(source);
            return target;
        }
    }
}
