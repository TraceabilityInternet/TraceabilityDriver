namespace TraceabilityDriver.Models.Traceback
{
    /// <summary>
    /// The kind of data cache resource a traceback touched.
    /// </summary>
    public enum TracebackItemType
    {
        /// <summary>
        /// An EPCIS event, identified by its event id.
        /// </summary>
        Event,

        /// <summary>
        /// A master data vocabulary element, identified by its element id.
        /// </summary>
        MasterData
    }
}
