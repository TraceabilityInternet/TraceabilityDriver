using OpenTraceability.Models.Identifiers;

namespace TraceabilityDriver.Models.Mapping
{
    /// <summary>
    /// A base class intended for common functionality across models. It serves as a foundation for other model classes.
    /// </summary>
    public class CommonBaseModel
    {
        /// <summary>
        /// This is used for building traceability identifiers using the GDST format. The traceability
        /// driver is only meant to build identifiers under a single domain and this is set in the 
        /// start up of the driver.
        /// </summary>
        public static string GDST_IDENTIFIERS_DOMAIN = "example.org";

        /// <summary>
        /// Determines if the owner Id is already a PGLN, if it is not, it generates one from the owner id.
        /// </summary>
        /// <param name="ownerId">The owner id.</param>
        /// <returns>A PGLN of the owner Id, either parsed or generated.</returns>
        public PGLN GeneratePGLN(string ownerId)
        {
            if (PGLN.IsPGLN(ownerId))
            {
                return new PGLN(ownerId);
            }
            else
            {
                // strip any non-numeric / alphabet characters out of the owner id
                string pglnStr = $"urn:gdst:{GDST_IDENTIFIERS_DOMAIN}:party:{NormalizeString(ownerId)}.0";
                return new PGLN(pglnStr);
            }
        }

        public string NormalizeString(string value)
        {
            return value.Where(char.IsLetterOrDigit).Aggregate("", (current, c) => current + c);
        }
    }
}
