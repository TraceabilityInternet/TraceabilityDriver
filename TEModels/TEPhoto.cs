using TraceabilityEngine.Interfaces.Models.Common;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Models
{
    public class TEPhoto : ITEPhoto
    {
        public Uri URI { get; set; }
        public Uri URL { get; set; }
        public string FileName { get; set; }
        public TEMeasurement Size { get; set; }
        public int PixelHeight { get; set; }
        public int PixelWidth { get; set; }
    }
}
