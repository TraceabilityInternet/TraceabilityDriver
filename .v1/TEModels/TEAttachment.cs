using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.StaticData;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Util.StaticData;

namespace TraceabilityEngine.Models
{
    public class TEAttachment : ITEAttachment
    {
        public AttachmentType AttachmentType { get; set; }
        public string FileName { get; set; }
        public Uri URI { get; set; }
        public Uri URL { get; set; }
        public TEMeasurement Size { get; set; }
        public TELanguage Language { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; }
        public int PixelWidth { get; set; }
        public int PixelHeight { get; set; }
    }
}
