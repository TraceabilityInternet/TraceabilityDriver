using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace FDModels.Products
{
    [DataContract]
    public class FDCategoryProfileSummary
    {
        [DataMember]
        public string URI { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}
