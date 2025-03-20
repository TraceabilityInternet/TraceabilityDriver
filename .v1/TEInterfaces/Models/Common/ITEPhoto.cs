using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Interfaces.Models.Common
{
    public interface ITEPhoto
    {
        Uri URI { get; set; }
        Uri URL { get; set; }
        string FileName { get; set; }
        TEMeasurement Size { get; set; }
        int PixelHeight { get; set; }
        int PixelWidth { get; set; }
    }
}
