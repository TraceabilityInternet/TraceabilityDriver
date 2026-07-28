using OpenTraceability.Models.Events.KDEs;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for a master data certification list.
    /// </summary>
    public static class CertificationListExtensions
    {
        /// <summary>
        /// Adds the source certificates that the target does not already carry.
        /// </summary>
        /// <remarks>
        /// Certificates are matched on their (type, standard, identification) triple, which is what identifies
        /// a certificate in the GDST mappings. Individual certificates are deliberately not deep-merged: a
        /// certificate is mapped from a single row, so a partially populated one never appears. The target must
        /// be non-null; a caller holding a null list has to assign the source itself.
        /// </remarks>
        /// <param name="target">The certification list to merge into. Modified in place.</param>
        /// <param name="source">The certification list to merge from. Not modified.</param>
        public static void Merge(this CertificationList target, CertificationList? source)
        {
            if (source?.Certificates == null)
            {
                return;
            }

            foreach (var sourceCertificate in source.Certificates)
            {
                bool alreadyPresent = target.Certificates.Any(c => c.CertificateType == sourceCertificate.CertificateType
                    && c.Standard == sourceCertificate.Standard
                    && c.Identification == sourceCertificate.Identification);

                if (!alreadyPresent)
                {
                    target.Certificates.Add(sourceCertificate);
                }
            }
        }
    }
}
