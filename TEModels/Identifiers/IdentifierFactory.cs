using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Models.Identifiers
{
    public static class IdentifierFactory
    {
        public static IGTIN ParseGTIN(string gtinStr, out string error)
        {
            error = null;
            IGTIN gtin = null;
            GTIN.TryParse(gtinStr, out gtin, out error);
            return gtin;
        }

        public static IGTIN ParseGTIN(string gtinStr)
        {
            IGTIN gtin = null;
            GTIN.TryParse(gtinStr, out gtin, out string error);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new TEException("Failed to parse GTIN. " + error);
            }
            return gtin;
        }

        public static IEPC ParseEPC(string epcStr, out string error)
        {
            error = null;
            IEPC epc = null;
            EPC.TryParse(epcStr, out epc, out error);
            return epc;
        }

        public static IGLN ParseGLN(string glnStr, out string error)
        {
            error = null;
            IGLN gln = null;
            GLN.TryParse(glnStr, out gln, out error);
            return gln;
        }

        public static IGLN ParseGLN(string glnStr)
        {
            IGLN gln = null;
            GLN.TryParse(glnStr, out gln, out string error);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new TEException("Failed to parse GLN. " + error);
            }
            return gln;
        }

        public static IPGLN ParsePGLN(string pglnStr, out string error)
        {
            error = null;
            IPGLN pgln = null;
            PGLN.TryParse(pglnStr, out pgln, out error);
            return pgln;
        }

        public static IPGLN ParsePGLN(string pglnStr)
        {
            IPGLN pgln = null;
            PGLN.TryParse(pglnStr, out pgln, out string error);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new TEException("Failed to parse PGLN. " + error);
            }
            return pgln;
        }
    }
}
