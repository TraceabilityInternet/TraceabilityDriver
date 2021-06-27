using TraceabilityEngine.Interfaces.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace TraceabilityEngine.Models
{
    public class TEAddress : TEAnonymizedAddress, ITEAddress
    {
        [Required(ErrorMessage = "The Address 1 is required.")]
        public string Address1 { get; set; }
        public string Address2 { get; set; }

        [Required(ErrorMessage = "The City is required.")]
        public string City { get; set; }

        [Required(ErrorMessage = "The State is required.")]
        public string State { get; set; }

        public string County { get; set; }

        public override string ToString()
        {
            List<string> pieces = new List<string>()
            {
                Address1,
                Address2,
                City,
                County,
                State,
                ZipCode,
                Country?.Abbreviation
            };
            string addressStr = string.Join(", ", pieces.Where(p => !string.IsNullOrWhiteSpace(p)));
            return addressStr;
        }
    }
}
