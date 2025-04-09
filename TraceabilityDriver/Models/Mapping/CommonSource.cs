namespace TraceabilityDriver.Models.Mapping
{
    public class CommonSource : CommonBaseModel
    {
        public string? Type { get; set; } = null;

        public string? Source { get; set; } = null;

        /// <summary>
        /// Merges data from another CommonSource object into this instance.
        /// Only null or empty properties in the current instance will be updated with non-null values from the source.
        /// </summary>
        /// <param name="source">The source CommonParty object to merge data from.</param>
        /// <exception cref="ArgumentNullException">Thrown if the source parameter is null.</exception>
        public void Merge(CommonSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            // Only update Name if current value is null or empty
            if(this.Type?.Equals(source.Type, StringComparison.OrdinalIgnoreCase) == true && string.IsNullOrWhiteSpace(this.Source) && !string.IsNullOrWhiteSpace(source.Source))
            {
                this.Source = source.Source;
            }
        }
    }
}
