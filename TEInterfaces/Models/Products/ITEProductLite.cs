using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductLite
    {
        long ID { get; set; }
        IGTIN GTIN { get; set; }
        string Name { get; set; }
        bool IsPublic { get; set; }
        string Description { get; set; }
        public bool Archived { get; set; }
    }
}
