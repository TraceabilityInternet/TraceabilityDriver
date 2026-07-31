namespace TraceabilityDriver.Services
{
    /// <summary>
    /// The normalized values of a single EPCIS MATCH_* query parameter, split into exact values and
    /// prefix values so database backends can translate the EPCIS trailing-* wildcard uniformly.
    /// </summary>
    public class EpcMatchValues
    {
        /// <summary>
        /// The lowercased EPC values that must match exactly.
        /// </summary>
        public List<string> ExactValues { get; } = new List<string>();

        /// <summary>
        /// The lowercased EPC prefixes produced from values ending in the * wildcard.
        /// </summary>
        public List<string> Prefixes { get; } = new List<string>();

        /// <summary>
        /// Whether the parameter carried any values and therefore must be applied as a filter.
        /// </summary>
        public bool HasValues
        {
            get { return ExactValues.Count > 0 || Prefixes.Count > 0; }
        }

        /// <summary>
        /// Normalizes the raw MATCH_* parameter values into exact values and wildcard prefixes.
        /// </summary>
        /// <param name="values">The raw parameter values as received in the query.</param>
        /// <returns>The normalized match values.</returns>
        public static EpcMatchValues Create(List<string> values)
        {
            EpcMatchValues matchValues = new EpcMatchValues();

            foreach (string value in values.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                string normalized = value.Trim().ToLower();

                // The EPCIS MATCH_* parameters allow a trailing * wildcard that matches any serial
                // component; everything before the wildcard is treated as a prefix.
                if (normalized.EndsWith('*'))
                {
                    matchValues.Prefixes.Add(normalized.Substring(0, normalized.Length - 1));
                }
                else
                {
                    matchValues.ExactValues.Add(normalized);
                }
            }

            return matchValues;
        }
    }
}
