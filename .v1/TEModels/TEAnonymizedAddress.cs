using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.Interfaces.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace TraceabilityEngine.Models
{
    public class TEAnonymizedAddress : ITEAnonymizedAddress
    {
        [Required(ErrorMessage = "The Zip Code  / Postal Code is required.")]
        public string ZipCode { get; set; }

        [Required(ErrorMessage = "The Country is required.")]
        public Country Country { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is ITEAnonymizedAddress))
            {
                return false;
            }

            return (this.ZipCode == ((ITEAnonymizedAddress)obj).ZipCode && this.Country.Equals(((ITEAnonymizedAddress)obj).Country));
        }

        public bool Equals(ITEAnonymizedAddress other)
        {
            if (other == null)
            {
                return false;
            }

            return (this.ZipCode == other.ZipCode && this.Country == other.Country);
        }

        public override int GetHashCode()
        {
            return (this.ZipCode?.GetHashCode() ?? 0) + (this.Country?.GetHashCode() ?? 0);
        }
    }
}
