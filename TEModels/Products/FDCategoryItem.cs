using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace FDModels.Products
{
    [DataContract]
    public class FDCategoryItem
    {
        [DataMember]
        public long CategoryID { get; set; }

        [DataMember]
        public long ProductID { get; set; }

        [DataMember]
        public string GTIN { get; set; }

        [DataMember]
        public string ProductBrand { get; set; }

        [DataMember]
        public string ProductName { get; set; }

        [DataMember]
        public List<FDCategoryItemClaim> Claims { get; set; } = new List<FDCategoryItemClaim>();
    }

    [DataContract]
    public class FDCategoryItemClaim
    {
        [DataMember]
        public FDClaim Claim { get; set; }

        [DataMember]
        public bool IsTicked { get; set; }
    }
}
