using OpenTraceability.Utility;

namespace TraceabilityDriver.Extensions
{
    public static class MeasurementExtensions
    {
        public static void Merge(this Measurement? targetMeasurement, Measurement sourceMeasurement)
        {
            if (targetMeasurement == null && sourceMeasurement != null)
            {
                targetMeasurement = new Measurement(sourceMeasurement);
            }
            else if (targetMeasurement != null && sourceMeasurement != null)
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
}
