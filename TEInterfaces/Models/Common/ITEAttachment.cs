using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Interfaces.Models.Common
{
    public interface ITEAttachment
    {
        AttachmentType AttachmentType { get; set; }
        string FileName { get; set; }
        Uri URI { get; set; }
        Uri URL { get; set; }
        TELanguage Language { get; set; }
        TEMeasurement Size { get; set; }
        DateTime? StartDate { get; set; }
        DateTime? EndDate { get; set; }
        string Description { get; set; }
        int PixelWidth { get; set; }
        int PixelHeight { get; set; }
    }
}
