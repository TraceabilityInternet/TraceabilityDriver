namespace TraceabilityDriver.Models.Mapping
{
    public class CommonSource : CommonBaseModel
    {
        public CommonParty? Party { get; set; } = null;

        public CommonLocation? Location { get; set; } = null;

        /// <summary>
        /// Merges data from another CommonSource object into this instance.
        /// Only null or empty properties in the current instance will be updated with non-null values from the source.
        /// </summary>
        /// <param name="source">The source CommonParty object to merge data from.</param>
        /// <exception cref="ArgumentNullException">Thrown if the source parameter is null.</exception>
        public void Merge(CommonSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if(this.Party == null && source.Party != null)
            {
                this.Party = source.Party;
            }
            else if (this.Party != null && source.Party != null)
            {
                this.Party.Merge(source.Party);
            }

            if (this.Location == null && source.Location != null)
            {
                this.Location = source.Location;
            }
            else if (this.Location != null && source.Location != null)
            {
                this.Location.Merge(source.Location);
            }
        }
    }
}
