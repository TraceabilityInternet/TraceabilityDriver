using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Interfaces.Models.Locations
{
    public interface ITELocationLite
    {
        long ID { get; set; }
        bool IsPublic { get; set; }
        bool Archived { get; set; }
        IGLN GLN { get; set; }
        string Name { get; set; }
        string Description { get; set; }
    }
}
