namespace TraceabilityDriver.Models.Traceback
{
    /// <summary>
    /// The lifecycle status of a traceback run.
    /// </summary>
    public enum TracebackStatus
    {
        /// <summary>
        /// The traceback has been started and has not finished yet.
        /// </summary>
        InProgress,

        /// <summary>
        /// The traceback finished and every requested EPC was processed without errors.
        /// </summary>
        Completed,

        /// <summary>
        /// The traceback finished, but one or more EPCs or resolution steps reported errors.
        /// </summary>
        CompletedWithErrors,

        /// <summary>
        /// The traceback aborted due to an unhandled failure before it could finish.
        /// </summary>
        Failed
    }
}
