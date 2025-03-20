using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace FDModels.Products
{
    [DataContract]
    public class FDClaim
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public string URI { get; set; }

        [DataMember]
        public string CategoryURI { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Column { get; set; }

        [DataMember]
        public string Table { get; set; }
    }
}
