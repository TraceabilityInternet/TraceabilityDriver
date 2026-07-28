using OpenTraceability.Utility;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for a quantity measurement.
    /// </summary>
    public static class MeasurementExtensions
    {
        /// <summary>
        /// Fills the target measurement's unset value and unit of measure from the source measurement.
        /// </summary>
        /// <remarks>
        /// A zero value counts as unset, because a measurement mapped from a row that did not carry the
        /// quantity comes through as zero rather than null. The target must be non-null; a caller holding a
        /// null measurement has to assign the source itself, since an extension method cannot hand a new
        /// instance back.
        /// </remarks>
        /// <param name="targetMeasurement">The measurement to merge into. Modified in place.</param>
        /// <param name="sourceMeasurement">The measurement to merge from. Not modified.</param>
        public static void Merge(this Measurement targetMeasurement, Measurement sourceMeasurement)
        {
            if (targetMeasurement.Value == 0 && sourceMeasurement.Value != 0)
            {
                targetMeasurement.Value = sourceMeasurement.Value;
            }

            if (targetMeasurement.UoM == null && sourceMeasurement.UoM != null)
            {
                targetMeasurement.UoM = sourceMeasurement.UoM;
            }
        }
    }
}
