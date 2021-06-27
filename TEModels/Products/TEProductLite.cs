using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Models.Products;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductLite : ITEProductLite
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public bool IsPublic { get; set; }

        [DataMember]
        [Required]
        public IGTIN GTIN { get; set; }

        [DataMember]
        [Required]
        public string Name { get; set; }

        [DataMember]
        public bool Archived { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}
