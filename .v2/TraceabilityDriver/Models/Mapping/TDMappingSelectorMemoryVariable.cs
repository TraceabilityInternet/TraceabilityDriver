namespace TraceabilityDriver.Models.Mapping
{
    /// <summary>
    /// Represents a memory variable used in TD mapping selection. It is part of a system for managing variable states.
    /// </summary>
    public class TDMappingSelectorMemoryVariable
    {
        /// <summary>
        /// The data type of the memory variable.
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// The defalt value for the memory variable.
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// The field to pull the value from in the data row.
        /// </summary>
        public string Field { get; set; } = string.Empty;
    }
}
