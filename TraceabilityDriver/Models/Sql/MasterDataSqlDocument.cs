using System;

namespace TraceabilityDriver.Models.Sql
{
    public class MasterDataSqlDocument
    {
        public long ID { get; set; }

        public string ElementId { get; set; } = string.Empty;

        public string ElementType { get; set; } = string.Empty;

        public string ElementJson { get; set; } = string.Empty;
    }
} 