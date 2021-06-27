using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace FDModels.Products
{
    [DataContract]
    public class FDProductSearchResult
    {
        [DataMember]
        public FDProductAvailability ProductAvailability { get; set; }

        [DataMember]
        public List<string> Info { get; set; }
    }
}
