using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace FDModels.Products
{
    [DataContract]
    public class FDCategoryItemOptions
    {
        [DataMember]
        public int PageNumber { get; set; }

        [DataMember]
        public int PageSize { get; set; }

        [DataMember]
        public string Country { get; set; }

        [DataMember]
        public string Zipcode { get; set; }

        [DataMember]
        public List<FDClaim> Claims { get; set; } = new List<FDClaim>();
    }
}
