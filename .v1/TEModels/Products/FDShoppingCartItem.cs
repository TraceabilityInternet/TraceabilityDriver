using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace FDModels.Products
{
    [DataContract]
    public class FDShoppingCartItem
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public long UserID { get; set; }

        [DataMember]
        public FDProductAvailability ProductAvailability { get; set; }

        [DataMember]
        public int Amount { get; set; }
    }
}
